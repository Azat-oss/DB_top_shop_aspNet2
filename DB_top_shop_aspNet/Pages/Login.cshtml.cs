using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DB_top_shop_aspNet.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        public User InputUser { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public LoginModel(ApplicationDbContext context, ILogger<LoginModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void OnGet()
        {
            HttpContext.Session.Clear();
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(InputUser.UserName) || string.IsNullOrEmpty(InputUser.PasswordInput))
            {
                ErrorMessage = "Заполните логин и пароль";
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == InputUser.UserName);

            if (user is null)
            {
                ErrorMessage = "Пользователь не найден!";
                _logger.LogWarning("Попытка входа несуществующего пользователя: {UserName}", InputUser.UserName);
                return Page();
            }

            if (!user.VerifyPassword(InputUser.PasswordInput))
            {
                ErrorMessage = "Неверный пароль!";
                _logger.LogWarning("Неверный пароль для пользователя: {UserName}", InputUser.UserName);
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("UserId", user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            HttpContext.Session.SetInt32("UserId", user.Id);
            _logger.LogInformation("Пользователь {UserName} вошел.", user.UserName);

            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }

    }
}
