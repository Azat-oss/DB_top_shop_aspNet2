using DB_top_shop_aspNet;          // ← ДОБАВЛЕНО для доступа к OrdersREST
using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;     // ← Нужно для AddSwaggerGen
using Serilog;
using Serilog.Events;
using SQLitePCL;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;


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

// === JWT Configuration ===
var jwtConfig = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtConfig["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

// === РЕГИСТРАЦИЯ СЕРВИСОВ ===
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// === Аутентификация: Cookie (Pages) + JWT (API) ===
builder.Services.AddAuthentication(options =>
{
    // По умолчанию для всего приложения — Cookie (для Razor Pages)
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
// 🔹 Cookie Authentication (для Razor Pages)
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Login";
    options.AccessDeniedPath = "/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "TopShop.Auth";
})
// 🔹 JWT Bearer Authentication (для REST API)
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
                context.Response.Headers.Append("Token-Expired", "true");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Поддержка токена в query string для Swagger
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.Request.Path.StartsWithSegments("/api"))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
});

// === Политики авторизации ===
builder.Services.AddAuthorization(options =>
{
    // 🔹 Для Cookie (Razor Pages)
    options.AddPolicy("AdminOnly", p =>
        p.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", p =>
        p.RequireRole("Manager", "Admin"));
    options.AddPolicy("UserOrHigher", p =>
        p.RequireRole("User", "Manager", "Admin"));

    // 🔹 Для JWT (REST API) — с явным указанием схемы аутентификации
    options.AddPolicy("ApiAdminOnly", p =>
    {
        p.RequireRole("Admin");
        p.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
    });
    options.AddPolicy("ApiManagerAdmin", p =>
    {
        p.RequireRole("Manager", "Admin");
        p.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
    });
    options.AddPolicy("ApiUserAny", p =>
    {
        p.RequireRole("User", "Manager", "Admin");
        p.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
    });
});

// Session (для дополнительных данных, не для аутентификации)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// === Swagger с поддержкой JWT ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TopShop API",
        Version = "v1",
        Description = "REST API для управления заказами с JWT-аутентификацией"
    });

    // 🔐 Кнопка авторизации в Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен в формате: `Bearer {your_token}`"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // XML-комментарии (опционально, если включены в .csproj)
    // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
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
        var created = context.Database.EnsureCreated();
        if (created)
            initLogger.LogInformation("База данных создана.");

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
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // 🔹 Swagger только в Development
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TopShop API v1");
        options.RoutePrefix = "swagger"; // Доступ по https://localhost:7151/swagger
    });
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

// 🔐 Порядок middleware — КРИТИЧЕН!
app.UseAuthentication();    // 1. Аутентификация
app.UseAuthorization();     // 2. Авторизация  
app.UseSession();           // 3. Session

app.MapRazorPages();

// 🔹 Регистрация REST API endpoints
app.MapOrdersApi(); // ← Твой extension method из OrdersREST.cs

// === ЗАПУСК ===
try
{
    var swaggerUrl = app.Urls.FirstOrDefault()?.TrimEnd('/') + "/swagger";
    Log.Information("🚀 Приложение запущено. Swagger: {SwaggerUrl}", swaggerUrl);
    Log.Information("📡 API endpoints: {Prefix}/api/orders", app.Urls.FirstOrDefault()?.TrimEnd('/'));

    //app.Run();
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