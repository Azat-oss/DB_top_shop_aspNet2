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
using System.Threading.Tasks;

namespace DB_top_shop_aspNet.Pages.Orders
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public Order Order { get; set; } = new();

        public SelectList ClientsSelectList { get; set; } = new SelectList(new List<Client>(), "Id", "Name");
        public SelectList ProductsSelectList { get; set; } = new SelectList(new List<Product>(), "Id", "Name");

        public async Task OnGetAsync()
        {
            await LoadSelectListsAsync();

            //var clients = await _context.Clients.ToListAsync();
            //var products = await _context.Products.ToListAsync();

            //ClientsSelectList = new SelectList(clients, "Id", "Name");
            //ProductsSelectList = new SelectList(products, "Id", "Name");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Order.Client");
            ModelState.Remove("Order.Product");
            // Важно: не биндим CreatedByUserId из формы, чтобы пользователь не подменил его
            ModelState.Remove("Order.CreatedByUserId");

            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            // 🔥 ОПРЕДЕЛЯЕМ ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ
            var user = _httpContextAccessor.HttpContext?.User;
            int currentUserId = 0;

            var userIdClaim = user?.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedId))
            {
                currentUserId = parsedId;
            }
            else
            {
                // Fallback
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == user.Identity.Name);
                if (dbUser == null) return RedirectToPage("/Login");
                currentUserId = dbUser.Id;
            }

            // Присваиваем ID создателя
            Order.CreatedByUserId = currentUserId;

            try
            {
                _context.Orders.Add(Order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Заказ создан пользователем ID {UserId}", currentUserId);
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания заказа");
                await LoadSelectListsAsync();
                ModelState.AddModelError("", "Не удалось создать заказ.");
                return Page();
            }
        }

        private async Task LoadSelectListsAsync()
        {
            var clients = await _context.Clients.ToListAsync();
            var products = await _context.Products.ToListAsync();
            ClientsSelectList = new SelectList(clients, "Id", "Name");
            ProductsSelectList = new SelectList(products, "Id", "Name");
        }

    }
}
