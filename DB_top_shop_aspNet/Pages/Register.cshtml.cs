using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DB_top_shop_aspNet.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegisterModel> _logger;

        [BindProperty]
        public User InputUser { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public RegisterModel(ApplicationDbContext context, ILogger<RegisterModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostCreate()
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(InputUser.UserName) || string.IsNullOrEmpty(InputUser.PasswordInput))
            {
                ErrorMessage = "Заполните все поля";
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.UserName == InputUser.UserName))
            {
                ErrorMessage = "Пользователь с таким именем уже существует!";
                return Page();
            }

            var newUser = new User
            {
                UserName = InputUser.UserName,
                Role = InputUser.Role,



                PasswordHash = ""
            };

            newUser.SetPassword(InputUser.PasswordInput);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Новый пользователь зарегистрирован: {UserName}", newUser.UserName);
            return RedirectToPage("/Login");
        }
    }
}
