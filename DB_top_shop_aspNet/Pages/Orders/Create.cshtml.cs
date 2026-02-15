using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DB_top_shop_aspNet.Pages.Orders
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Order Order { get; set; } = new();

        public SelectList ClientsSelectList { get; set; } = new SelectList(new List<Client>(), "Id", "Name");
        public SelectList ProductsSelectList { get; set; } = new SelectList(new List<Product>(), "Id", "Name");

        public async Task OnGetAsync()
        {
            var clients = await _context.Clients.ToListAsync();
            var products = await _context.Products.ToListAsync();

            ClientsSelectList = new SelectList(clients, "Id", "Name");
            ProductsSelectList = new SelectList(products, "Id", "Name");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 🔧 Удаляем навигационные свойства из ModelState
            ModelState.Remove("Order.Client");
            ModelState.Remove("Order.Product");

            if (!ModelState.IsValid)
            {
                // Логируем ошибки валидации
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        _logger.LogWarning("Ошибка валидации для поля {Field}: {Error}", key, errors[0].ErrorMessage);
                    }
                }

                // Перезагружаем списки
                var clients = await _context.Clients.ToListAsync();
                var products = await _context.Products.ToListAsync();
                ClientsSelectList = new SelectList(clients, "Id", "Name");
                ProductsSelectList = new SelectList(products, "Id", "Name");

                return Page();
            }

            try
            {
                _context.Orders.Add(Order);
                await _context.SaveChangesAsync();

                // Логируем успешное создание
                _logger.LogInformation("Заказ для клиента ID {ClientId} (Товар ID: {ProductId}) успешно создан.", Order.ClientId, Order.ProductId);

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // Логируем ошибку при сохранении
                _logger.LogError(ex, "Ошибка при создании заказа для клиента ID {ClientId}.", Order.ClientId);

                // Перезагружаем списки перед показом ошибки
                var clients = await _context.Clients.ToListAsync();
                var products = await _context.Products.ToListAsync();
                ClientsSelectList = new SelectList(clients, "Id", "Name");
                ProductsSelectList = new SelectList(products, "Id", "Name");

                ModelState.AddModelError(string.Empty, "Не удалось создать заказ. Проверьте данные и повторите попытку.");
                return Page();

            }
        }

    }
}
