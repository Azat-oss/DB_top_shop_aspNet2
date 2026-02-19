using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DB_top_shop_aspNet.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        public User User { get; set; } = new();

        public LoginModel(ApplicationDbContext context, ILogger<LoginModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void OnGet()
        {
            HttpContext.Session.Clear();
        }

        public IActionResult OnPostLogin()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ищем пользователя с этим именем
            var user = _context.Users.FirstOrDefault(u => u.UserName == User.UserName);

            if (user is null)
            {
                ModelState.AddModelError("User.UserName", "Пользователь не найден !");
                return Page();
            }
            else
            {
                // проверка введенного пароля
                if (user.Password != User.Password)
                {
                    ModelState.AddModelError("User.UserName", "Пароль не верен !");
                    return Page();
                }

                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("UserRole", user.Role.ToString());
                HttpContext.Session.SetInt32("UserId", user.Id);

                _logger.LogInformation($"{User.ToString()} вошел в систему !");
                return RedirectToPage("/Index");
            }
        }


        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            _logger.LogInformation("Пользователь вышел из системы");
            return RedirectToPage("/Index");
        }



    }
}
