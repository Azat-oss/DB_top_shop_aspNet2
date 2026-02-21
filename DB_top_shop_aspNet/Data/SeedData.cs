using Bogus;
using DB_top_shop_aspNet.Models;
using Microsoft.EntityFrameworkCore;
using static DB_top_shop_aspNet.Models.User;

namespace DB_top_shop_aspNet.Data
{
    public static class SeedData
    {
        // ИЗМЕНЕНО: Принимаем сразу контекст, а не IServiceProvider
        public static void Initialize(ApplicationDbContext context)
        {
            Console.WriteLine("SeedData: Старт...");

            // Проверяем, есть ли данные (контекст уже открыт)
            // Используем Any(), так как это быстрее, чем Count() для проверки наличия
            bool hasData = context.Clients.Any() || context.Products.Any() || context.Orders.Any();

            if (hasData)
            {
                Console.WriteLine("SeedData: Данные уже существуют. Выход.");
                return;
            }

            Console.WriteLine("SeedData: Генерация Clients...");
            var clients = new Faker<Client>("ru")
                .RuleFor(c => c.Name, f => f.Name.FullName())
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .Generate(10);

            Console.WriteLine("SeedData: Генерация Products...");


            var foodProducts = new List<string>()
        {
            "Хлеб белый",
            "Молоко пастеризованное",
            "Сыр Российский",
            "Колбаса вареная Докторская",
            "Картофель свежий",
            "Морковь свежая",
            "Апельсины свежие",
            "Курица охлажденная",
            "Рыба минтай замороженная",
            "Макароны пшеничные",
            "Масло подсолнечное рафинированное",
            "Чай черный листовой",
            "Сахар-песок",
            "Мед натуральный",
            "Соль поваренная"
        };

            var products = new Faker<Product>("ru")
                //.RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Name, f => f.PickRandom(foodProducts))
                .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(1, 1000), 2))
                .Generate(10);




            Console.WriteLine("SeedData: Сохранение Clients и Products...");
            context.Clients.AddRange(clients);
            context.Products.AddRange(products);

            // Этот SaveChanges VERY важен - он присваивает ID
            int saved1 = context.SaveChanges();
            Console.WriteLine($"SeedData: Сохранено объектов: {saved1}");

            // Получаем ID
            var clientIds = context.Clients.Select(c => c.Id).ToList();
            var productIds = context.Products.Select(p => p.Id).ToList();

            if (!clientIds.Any() || !productIds.Any())
            {
                Console.WriteLine("SeedData: ОШИБКА! Списки ID пусты после сохранения.");
                return;
            }

            Console.WriteLine("SeedData: Генерация Orders...");


            var orders = new Faker<Order>()
    .RuleFor(o => o.Date, f => (DateTime?)f.Date.Between(
    DateTime.Now.AddYears(-2),
    DateTime.Now
).Date)


    .RuleFor(o => o.ClientId, f => f.PickRandom(clientIds))
    .RuleFor(o => o.ProductId, f => f.PickRandom(productIds))
    .RuleFor(o => o.Quantity, f => f.Random.Int(1, 10))
    .Generate(10);

            Console.WriteLine("SeedData: Сохранение Orders...");
            context.Orders.AddRange(orders);
            int saved2 = context.SaveChanges();
            Console.WriteLine($"SeedData: Сохранено заказов: {saved2}");

            Console.WriteLine("SeedData: Успешно завершено.");
            var users = new List<User>
            { new() { UserName = "admin", Role = Roles.Admin },
              new() { UserName = "manager", Role = Roles.Manager },
              new() { UserName = "guest",  Role = Roles.User }
            };

            foreach (var user in users)
            {
                user.SetPassword(user.UserName); // пароль = логин для простоты
            }


            context.Users.AddRange(users);
            context.SaveChanges();

        }
    }
}