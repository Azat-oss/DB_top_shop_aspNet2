using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;

namespace DB_top_shop_aspNet.Pages.Orders
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Order Order { get; set; } = new();

        public SelectList ClientsSelectList { get; set; } = new SelectList(new List<Client>(), "Id", "Name");
        public SelectList ProductsSelectList { get; set; } = new SelectList(new List<Product>(), "Id", "Name");

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Попытка редактирования несуществующего заказа с ID {OrderId}.", id.Value);
                return NotFound();
            }

            Order = order;

            var clients = await _context.Clients.ToListAsync();
            var products = await _context.Products.ToListAsync();

            ClientsSelectList = new SelectList(clients, "Id", "Name", order.ClientId);
            ProductsSelectList = new SelectList(products, "Id", "Name", order.ProductId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Удаляем навигационные свойства
            ModelState.Remove("Order.Client");
            ModelState.Remove("Order.Product");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации при редактировании заказа ID {OrderId}.", Order.Id);

                var clients = await _context.Clients.ToListAsync();
                var products = await _context.Products.ToListAsync();
                ClientsSelectList = new SelectList(clients, "Id", "Name", Order.ClientId);
                ProductsSelectList = new SelectList(products, "Id", "Name", Order.ProductId);

                return Page();
            }

            try
            {
                _context.Attach(Order).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Заказ с ID {OrderId} успешно обновлён.", Order.Id);
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(Order.Id))
                {
                    _logger.LogWarning("Заказ с ID {OrderId} был удалён другим пользователем во время редактирования.", Order.Id);
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении заказа с ID {OrderId}.", Order.Id);

                var clients = await _context.Clients.ToListAsync();
                var products = await _context.Products.ToListAsync();
                ClientsSelectList = new SelectList(clients, "Id", "Name", Order.ClientId);
                ProductsSelectList = new SelectList(products, "Id", "Name", Order.ProductId);

                ModelState.AddModelError(string.Empty, "Не удалось сохранить изменения. Повторите попытку.");
                return Page();
            }
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
