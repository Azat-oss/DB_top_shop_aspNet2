using DB_top_shop_aspNet.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using SQLitePCL;
using System.Reflection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

//ЕСЛИ вдруг не работает SQLite, как то помогло!!! 13_02_2026
//if (builder.Configuration["ActiveDatabase"] == "SQLite")
//{
//    Batteries.Init();
//}

// === Настройка Serilog ===
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
        retainedFileCountLimit: 7, // хранить последние 7 дней
        fileSizeLimitBytes: 10_000_000, // ~10 МБ на файл
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}")
    .CreateBootstrapLogger();

builder.Host.UseSerilog(); // Подключаем Serilog как основной ILoggerProvider



// Читаем настройки
var configuration = builder.Configuration;
string? conPostgres = configuration.GetConnectionString("Postgres");
string? conSQLite = configuration.GetConnectionString("SQLite");
string? conSqlExpress = configuration.GetConnectionString("SqlExpress");
string? activeDb = configuration["ActiveDatabase"] ?? "Postgres";

// Логируем, какую базу мы выбрали

Console.WriteLine($"--- КОНФИГУРАЦИЯ ---");
Console.WriteLine($"Выбранная БД: {activeDb}");
Console.WriteLine($"Строка подключения: {activeDb switch { "Postgres" => conPostgres, "SQLite" => conSQLite, _ => conSqlExpress }}");

// Регистрация DbContext
switch (activeDb)
{
    case "Postgres":
        if (string.IsNullOrEmpty(conPostgres)) throw new InvalidOperationException("Postgres connection string is empty.");
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(conPostgres));
        break;
    case "SQLite":
        if (string.IsNullOrEmpty(conSQLite)) throw new InvalidOperationException("SQLite connection string is empty.");
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(conSQLite));
        break;
    case "SqlExpress":
        if (string.IsNullOrEmpty(conSqlExpress)) throw new InvalidOperationException("SqlExpress connection string is empty.");
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(conSqlExpress));
        break;
    default:
        throw new InvalidOperationException($"Unknown database type: {activeDb}");
}

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Session работает даже без согласия на cookies
});

var app = builder.Build();
Console.WriteLine($"=== СРЕДА ЗАПУСКА: {app.Environment.EnvironmentName} ===");


// --- БЛОК ИНИЦИАЛИЗАЦИИ ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var initLogger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var created = context.Database.EnsureCreated();

        if (created)
            initLogger.LogInformation("База данных создана.");

        var clientCount = context.Clients.Count();
        if (clientCount == 0)
        {
            SeedData.Initialize(context);
            initLogger.LogInformation("Выполнено начальное заполнение (seeding).");
        }
        else
        {
            initLogger.LogInformation("Таблицы уже содержат данные ({ClientCount} клиентов).", clientCount);
        }
    }
    catch (Exception ex)
    {
        initLogger.LogCritical(ex, "Ошибка при инициализации базы данных!");
        throw;
    }
}



// === Middleware Pipeline ===
if (app.Environment.IsDevelopment())  //убрал !app.Environment.IsDevelopment()
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");
    app.UseHsts();
}
else
{
    // В Development можно оставить Developer Exception Page
    app.UseDeveloperExceptionPage();
}



app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapRazorPages();


try
{
    Log.Information("Запуск веб-приложения...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с фатальной ошибкой.");
    throw;
}
finally
{
    Log.CloseAndFlush(); 
}

app.Run();