using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;

namespace DB_top_shop_aspNet.Pages.Orders
{
    public class DeleteModel : PageModel
    {
        private readonly DB_top_shop_aspNet.Data.ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;
        public DeleteModel(ApplicationDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Order Order { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Попытка удаления заказа с пустым ID.");
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Попытка удаления несуществующего заказа с ID {OrderId}.", id.Value);
                return NotFound();
            }

            Order = order;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var order = await _context.Orders.FindAsync(id);

                if (order != null)
                {
                    Order = order; // Сохраняем для логирования
                    _context.Orders.Remove(Order);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Заказ с ID {OrderId} успешно удалён.", order.Id);
                }
                else
                {
                    _logger.LogWarning("Заказ с ID {OrderId} не найден при попытке удаления.", id.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении заказа с ID {OrderId}.", id);
                // Добавляем ошибку в модель, если планируем остаться на странице (хотя здесь редирект)
                // ModelState.AddModelError(string.Empty, "Произошла ошибка при удалении.");
            }

            return RedirectToPage("./Index");
        }
    }
}
