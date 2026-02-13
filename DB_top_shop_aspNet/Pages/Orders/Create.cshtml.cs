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

        public CreateModel(ApplicationDbContext context) => _context = context;

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
            // 🔧 Удаляем навигационные свойства из ModelState, чтобы избежать ложных ошибок валидации
            ModelState.Remove("Order.Client");
            ModelState.Remove("Order.Product");

            if (!ModelState.IsValid)
            {
                // 🔍 (Опционально) Вывод ошибок в консоль для отладки
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        Console.WriteLine($"[Validation Error] {key}: {errors[0].ErrorMessage}");
                    }
                }

                // Перезагружаем выпадающие списки, иначе форма будет пустой
                var clients = await _context.Clients.ToListAsync();
                var products = await _context.Products.ToListAsync();
                ClientsSelectList = new SelectList(clients, "Id", "Name");
                ProductsSelectList = new SelectList(products, "Id", "Name");

                return Page(); // остаёмся на странице
            }

            // Сохраняем заказ
            _context.Orders.Add(Order);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");

        }
    }
}
