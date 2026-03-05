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
    //[Authorize(Policy = "ManagerOrAdmin")] // Менеджер видит, Админ делает всё
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly DB_top_shop_aspNet.Data.ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public IndexModel(DB_top_shop_aspNet.Data.ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public IList<Order> Order { get;set; } = default!;

        public async Task OnGetAsync()
        {
            //Order = await _context.Orders
            //    .Include(o => o.Client)
            //    .Include(o => o.Product).ToListAsync();

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                return; // Или редирект на логин
            }

            // Получаем ID текущего пользователя из Claims (мы сохраняли его при логине)
            // В LoginModel вы делали: new Claim("UserId", user.Id.ToString())
            var userIdClaim = user.FindFirst("UserId")?.Value;

            // Если по какой-то причине Claim нет, пробуем найти пользователя по имени и берем ID из БД
            int currentUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out currentUserId);
            }
            else
            {
                // Fallback: поиск по имени (менее эффективно, но надежно)
                var userName = user.Identity.Name;
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (dbUser != null) currentUserId = dbUser.Id;
            }

            // Получаем роль пользователя
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Order> query = _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Product);

            // 🔥 ЛОГИКА ФИЛЬТРАЦИИ
            if (userRole != "Admin" && userRole != "Manager")
            {
                // Если обычный User — показываем только ЕГО заказы
                query = query.Where(o => o.CreatedByUserId == currentUserId);
            }
            // Если Admin или Manager — query остается без фильтра (видят всё)

            Order = await query.ToListAsync();



        }
    }
}
