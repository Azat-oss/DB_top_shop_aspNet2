using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DB_top_shop_aspNet.Pages.Orders
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // 🔥 Добавляем IHttpContextAccessor в конструктор
        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public Order Order { get; set; } = new();

        public SelectList ClientsSelectList { get; set; } = new SelectList(new List<Client>(), "Id", "Name");
        public SelectList ProductsSelectList { get; set; } = new SelectList(new List<Product>(), "Id", "Name");

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                _logger.LogWarning("Заказ не найден.");
                return NotFound();
            }

            // 🔐 Проверка прав доступа (чтобы юзер не редактировал чужое)
            if (!await CheckAccessAsync(order))
            {
                return RedirectToPage("./Index");
            }

            Order = order;
            await LoadSelectListsAsync(order.ClientId, order.ProductId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Удаляем лишние поля из валидации
            ModelState.Remove("Order.Client");
            ModelState.Remove("Order.Product");
            // Важно: убираем CreatedByUserId из биндинга, чтобы нельзя было подменить через форму
            ModelState.Remove("Order.CreatedByUserId");

            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync(Order.ClientId, Order.ProductId);
                return Page();
            }

            // 🔥 КРИТИЧЕСКИЙ МОМЕНТ: Получаем оригинальный заказ из БД
            var existingOrder = await _context.Orders.FindAsync(Order.Id);

            if (existingOrder == null)
            {
                return NotFound();
            }

            // 🔐 Повторная проверка прав перед сохранением
            if (!await CheckAccessAsync(existingOrder))
            {
                return RedirectToPage("./Index");
            }

            try
            {
                // 🔥 ВОССТАНАВЛИВАЕМ поле CreatedByUserId из оригинала!
                // Иначе EF запишет 0 (значение по умолчанию из формы) и заказ "пропадет" у пользователя.
                Order.CreatedByUserId = existingOrder.CreatedByUserId;

                // Обновляем только нужные поля
                _context.Entry(existingOrder).CurrentValues.SetValues(Order);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Заказ {OrderId} обновлен.", Order.Id);
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(Order.Id)) return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения.");
                await LoadSelectListsAsync(Order.ClientId, Order.ProductId);
                ModelState.AddModelError("", "Не удалось сохранить изменения.");
                return Page();
            }
        }

        // 🔐 Метод проверки прав: Админ/Менеджер — все, Юзер — только свои
        private async Task<bool> CheckAccessAsync(Order order)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated) return false;

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Admin" || role == "Manager") return true;

            var userIdClaim = user.FindFirst("UserId")?.Value;
            int currentUserId = 0;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsed))
            {
                currentUserId = parsed;
            }
            else
            {
                // Fallback если Claim нет
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == user.Identity.Name);
                if (dbUser != null) currentUserId = dbUser.Id;
            }

            return order.CreatedByUserId == currentUserId;
        }

        private async Task LoadSelectListsAsync(int? clientId, int? productId)
        {
            var clients = await _context.Clients.ToListAsync();
            var products = await _context.Products.ToListAsync();
            ClientsSelectList = new SelectList(clients, "Id", "Name", clientId);
            ProductsSelectList = new SelectList(products, "Id", "Name", productId);
        }

        private bool OrderExists(int id) => _context.Orders.Any(e => e.Id == id);
    }
}
