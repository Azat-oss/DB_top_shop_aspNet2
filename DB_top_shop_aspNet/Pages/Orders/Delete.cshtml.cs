using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DB_top_shop_aspNet.Pages.Orders
{
    [Authorize] // Доступ только для авторизованных пользователей
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DeleteModel(
            ApplicationDbContext context,
            ILogger<DeleteModel> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public Order Order { get; set; } = default!;

        // 🔐 Вспомогательный метод: получение данных текущего пользователя
        private async Task<(int UserId, string? Role)> GetCurrentUserInfoAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
                return (0, null);

            var userIdClaim = user.FindFirst("UserId")?.Value;
            int currentUserId = 0;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedId))
            {
                currentUserId = parsedId;
            }
            else
            {
                // Fallback: поиск по имени пользователя
                var userName = user.Identity.Name;
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (dbUser != null) currentUserId = dbUser.Id;
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            return (currentUserId, role);
        }

        // 🔐 Вспомогательный метод: проверка прав доступа к заказу
        private bool CanAccessOrder(Order order, string? userRole, int currentUserId)
        {
            // Админ и Менеджер имеют доступ ко всем заказам
            if (userRole == "Admin" || userRole == "Manager")
                return true;

            // Обычный пользователь — только к своим
            return order.CreatedByUserId == currentUserId;
        }

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

            // 🔐 ПРОВЕРКА ПРАВ ДОСТУПА
            var (currentUserId, userRole) = await GetCurrentUserInfoAsync();
            if (!CanAccessOrder(order, userRole, currentUserId))
            {
                _logger.LogWarning("Пользователь {UserId} попытался получить доступ к чужому заказу {OrderId} для удаления.",
                    currentUserId, order.Id);
                return RedirectToPage("./Index");
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

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // 🔐 ПОВТОРНАЯ ПРОВЕРКА ПРАВ (защита от подмены ID в форме)
            var (currentUserId, userRole) = await GetCurrentUserInfoAsync();
            if (!CanAccessOrder(order, userRole, currentUserId))
            {
                _logger.LogWarning("Пользователь {UserId} попытался удалить чужой заказ {OrderId}.",
                    currentUserId, order.Id);
                return RedirectToPage("./Index");
            }

            try
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Заказ с ID {OrderId} успешно удалён пользователем {UserId}.",
                    order.Id, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении заказа с ID {OrderId}.", id);
                // Можно добавить ошибку в ModelState, если остаёмся на странице
                // ModelState.AddModelError(string.Empty, "Произошла ошибка при удалении.");
                // return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}
