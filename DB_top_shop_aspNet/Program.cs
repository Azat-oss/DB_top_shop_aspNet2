using DB_top_shop_aspNet.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SQLitePCL;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// === ИНИЦИАЛИЗАЦИЯ ===
var builder = WebApplication.CreateBuilder(args);

// Инициализация SQLite (если используется)
if (builder.Configuration["ActiveDatabase"] == "SQLite")
{
    Batteries.Init();
}

// === НАСТРОЙКА SERILOG ===
var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
if (!Directory.Exists(logsDirectory))
    Directory.CreateDirectory(logsDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: Path.Combine(logsDirectory, "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 10_000_000,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}")
    .CreateBootstrapLogger();

builder.Host.UseSerilog();

// === ЧТЕНИЕ НАСТРОЕК БД ===
var configuration = builder.Configuration;
string? conPostgres = configuration.GetConnectionString("Postgres");
string? conSQLite = configuration.GetConnectionString("SQLite");
string? conSqlExpress = configuration.GetConnectionString("SqlExpress");
string activeDb = configuration["ActiveDatabase"] ?? "Postgres";

Console.WriteLine($"--- КОНФИГУРАЦИЯ ---");
Console.WriteLine($"Выбранная БД: {activeDb}");

// === РЕГИСТРАЦИЯ DbContext ===
switch (activeDb)
{
    case "Postgres":
        if (string.IsNullOrEmpty(conPostgres))
            throw new InvalidOperationException("Postgres connection string is empty.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(conPostgres));
        break;

    case "SQLite":
        if (string.IsNullOrEmpty(conSQLite))
            throw new InvalidOperationException("SQLite connection string is empty.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(conSQLite));
        break;

    case "SqlExpress":
        if (string.IsNullOrEmpty(conSqlExpress))
            throw new InvalidOperationException("SqlExpress connection string is empty.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(conSqlExpress));
        break;

    default:
        throw new InvalidOperationException($"Unknown database type: {activeDb}");
}

// === РЕГИСТРАЦИЯ СЕРВИСОВ ===
builder.Services.AddRazorPages();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Пути к страницам входа/выхода (файлы лежат в /Pages/)
        options.LoginPath = "/Login";
        options.LogoutPath = "/Login";
        options.AccessDeniedPath = "/Login";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;

        // Для локальной разработки без HTTPS используйте None
        // Для продакшена с HTTPS используйте Always
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;

        // Имя куки (опционально)
        options.Cookie.Name = "TopShop.Auth";
    });

// Авторизация и политики
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));

    options.AddPolicy("UserOrHigher", policy =>
        policy.RequireRole("User", "Manager", "Admin"));
});

builder.Services.AddHttpContextAccessor();

// Session (для дополнительных данных, не для аутентификации)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// === ПОСТРОЕНИЕ ПРИЛОЖЕНИЯ ===
var app = builder.Build();

Console.WriteLine($"=== СРЕДА: {app.Environment.EnvironmentName} ===");

// === ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var initLogger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Создаёт БД и таблицы, если их нет
        var created = context.Database.EnsureCreated();
        if (created)
            initLogger.LogInformation("База данных создана.");

        // Seeding, если таблица Users пуста
        if (!context.Users.Any())
        {
            SeedData.Initialize(context);
            initLogger.LogInformation("Выполнено начальное заполнение (seeding).");
        }
        else
        {
            initLogger.LogInformation("База данных уже содержит данные.");
        }
    }
    catch (Exception ex)
    {
        initLogger.LogCritical(ex, "❌ Ошибка при инициализации базы данных!");
        throw;
    }
}

// === MIDDLEWARE PIPELINE ===

// Обработка ошибок
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔐 ВАЖНО: Порядок middleware!
app.UseAuthentication();    // Сначала аутентификация
app.UseAuthorization();     // Потом авторизация
app.UseSession();           // Session после Auth

app.MapRazorPages();

// === ЗАПУСК ===
try
{
    Log.Information("🚀 Запуск веб-приложения на портах: {Urls}",
        string.Join(", ", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Приложение завершилось с фатальной ошибкой.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

app.Run();