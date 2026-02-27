using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DB_top_shop_aspNet
{
    public static class OrdersREST
    {
        public static void MapOrdersApi(this WebApplication app)
        {
            var group = app.MapGroup("/api/orders")
                .WithTags("Orders")
                .WithOpenApi(); // Для Swagger

            // 🔹 GET все заказы — Manager/Admin
            group.MapGet("/", async (ApplicationDbContext db) =>
            {
                var orders = await db.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Product)
                    .ToListAsync();

                return Results.Ok(orders.Select(o => new OrderDto(o)));
            })
            .RequireAuthorization("ApiManagerAdmin")
            .Produces<List<OrderDto>>(200)
            .Produces(401)
            .WithName("GetAllOrders");

            // 🔹 GET по ID — Manager/Admin
            group.MapGet("/{id:int}", async (int id, ApplicationDbContext db) =>
            {
                var order = await db.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                return order is null
                    ? Results.NotFound(new { error = $"Заказ #{id} не найден" })
                    : Results.Ok(new OrderDto(order));
            })
            .RequireAuthorization("ApiManagerAdmin")
            .Produces<OrderDto>(200)
            .Produces(404)
            .Produces(401)
            .WithName("GetOrderById");

            // 🔹 POST создать — User/Manager/Admin
            group.MapPost("/", async (
                CreateOrderDto dto,
                ApplicationDbContext db,
                ClaimsPrincipal user) =>
            {
                // Валидация через DataAnnotations
                var errors = Validate(dto); // ← Твой существующий хелпер
                if (errors.Count > 0)
                    return Results.BadRequest(new ValidationProblemDetails(errors));

                // Проверка существования связанных сущностей
                if (!await db.Clients.AnyAsync(c => c.Id == dto.ClientId))
                    return Results.BadRequest(new { error = "Клиент не найден" });

                if (!await db.Products.AnyAsync(p => p.Id == dto.ProductId))
                    return Results.BadRequest(new { error = "Товар не найден" });

                var order = new Order
                {
                    Date = dto.Date.ToUniversalTime(),
                    ClientId = dto.ClientId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                    // UserId можно добавить, если нужно отслеживать создателя:
                    // UserId = int.Parse(user.FindFirst("UserId")?.Value ?? "0")
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                return Results.Created($"/api/orders/{order.Id}", new OrderDto(order));
            })
            .RequireAuthorization("ApiUserAny")
            .Produces<OrderDto>(201)
            .Produces(400)
            .Produces(401)
            .WithName("CreateOrder");

            // 🔹 PUT обновить — User/Manager/Admin
            group.MapPut("/{id:int}", async (
                int id,
                UpdateOrderDto dto,
                ApplicationDbContext db) =>
            {
                var errors = Validate(dto);  // dto — твой параметр метода
                if (errors.Count > 0)
                    return Results.BadRequest(new ValidationProblemDetails(errors));

                var order = await db.Orders.FindAsync(id);
                if (order is null)
                    return Results.NotFound(new { error = $"Заказ #{id} не найден" });

                if (!await db.Clients.AnyAsync(c => c.Id == dto.ClientId))
                    return Results.BadRequest(new { error = "Клиент не найден" });

                if (!await db.Products.AnyAsync(p => p.Id == dto.ProductId))
                    return Results.BadRequest(new { error = "Товар не найден" });

                order.Date = dto.Date.ToUniversalTime();
                order.ClientId = dto.ClientId;
                order.ProductId = dto.ProductId;
                order.Quantity = dto.Quantity;

                await db.SaveChangesAsync();

                return Results.Ok(new OrderDto(order));
            })
            .RequireAuthorization("ApiUserAny")
            .Produces<OrderDto>(200)
            .Produces(400)
            .Produces(404)
            .Produces(401)
            .WithName("UpdateOrder");

            // 🔹 DELETE — только Admin
            group.MapDelete("/{id:int}", async (int id, ApplicationDbContext db) =>
            {
                var order = await db.Orders.FindAsync(id);
                if (order is null)
                    return Results.NotFound(new { error = $"Заказ #{id} не найден" });

                db.Orders.Remove(order);
                await db.SaveChangesAsync();

                return Results.NoContent();
            })
            .RequireAuthorization("ApiAdminOnly")
            .Produces(204)
            .Produces(404)
            .Produces(401)
            .WithName("DeleteOrder");

            // 🔥 POST /api/orders/jwt — получение токена (публичный)
            group.MapPost("/jwt", async (
                LoginApiDto login,
                ApplicationDbContext db,
                IConfiguration config,
                ILogger<Program> logger) =>
            {
                if (string.IsNullOrWhiteSpace(login.UserName) || string.IsNullOrWhiteSpace(login.Password))
                    return Results.BadRequest(new { error = "Логин и пароль обязательны" });

                var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == login.UserName);
                if (user is null)
                {
                    logger.LogWarning("Попытка входа с несуществующим логином: {Login}", login.UserName);
                    return Results.BadRequest(new { error = "Пользователь не найден" });
                }

                // 🔐 Проверка пароля через BCrypt (как в LoginModel)
                if (!BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash)) // ← Исправлено: PasswordHash!
                {
                    logger.LogWarning("Неверный пароль для пользователя: {Login}", login.UserName);
                    return Results.BadRequest(new { error = "Неверный пароль" });
                }

                // 🎫 Генерация JWT
                var jwtConfig = config.GetSection("Jwt");
                var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]!);
                var expiresHours = jwtConfig.GetValue<int>("ExpiresHours", 1);

                var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("UserId", user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(expiresHours),
                    Issuer = jwtConfig["Issuer"],
                    Audience = jwtConfig["Audience"],
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                logger.LogInformation("✅ JWT выдан для {UserName} ({Role})", user.UserName, user.Role);

                return Results.Ok(new
                {
                    token = tokenString,
                    tokenType = "Bearer",
                    role = user.Role.ToString(),
                    userName = user.UserName,
                    expiresAt = DateTime.UtcNow.AddHours(expiresHours)
                });
            })
            .AllowAnonymous() // 🔓 Публичный endpoint
            .Produces(200)
            .Produces(400)
            .WithName("GetJwtToken");
        }

        // 🔧 Хелпер для валидации DTO (если не используешь ModelState)
        private static Dictionary<string, string[]> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);

            return results
                .SelectMany(r => r.MemberNames, (r, m) => new { MemberName = m, ErrorMessage = r.ErrorMessage })
                .GroupBy(x => x.MemberName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage!).ToArray());
        }
    }
}
