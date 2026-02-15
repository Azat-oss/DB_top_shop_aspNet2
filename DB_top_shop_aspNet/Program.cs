using DB_top_shop_aspNet.Data;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

var builder = WebApplication.CreateBuilder(args);

//ЕСЛИ вдруг не работает SQLite, как то помогло!!! 13_02_2026
//if (builder.Configuration["ActiveDatabase"] == "SQLite")
//{
//    Batteries.Init();
//}

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
var app = builder.Build();
Console.WriteLine($"=== СРЕДА ЗАПУСКА: {app.Environment.EnvironmentName} ===");
// --- БЛОК ИНИЦИАЛИЗАЦИИ ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var dbType = context.Database.ProviderName; // Проверяем, какой провайдер реально используется

       

        // 1. Создаем таблицы
        var created = context.Database.EnsureCreated();
       

        // 2. Проверяем наличие данных ПЕРЕД сидированием
        var clientCount = context.Clients.Count();
        

        if (clientCount == 0)
        {
            

            // !!! ИЗМЕНЕНИЕ: Передаем готовый контекст, а не serviceProvider
            SeedData.Initialize(context);

            

            // Проверяем ПОСЛЕ
            var newCount = context.Clients.Count();
           
        }
        else
        {
            
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"!!! КРИТИЧЕСКАЯ ОШИБКА !!!");
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);
    }
}

// --- PIPELINE ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); 
    app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}"); 
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();