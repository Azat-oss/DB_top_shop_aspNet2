using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DB_top_shop_aspNet.Pages
{
    public class IndexModel : PageModel
    {

        //private readonly ILogger<IndexModel> _logger;

        //public IndexModel(ILogger<IndexModel> logger)
        //{
        //    _logger = logger;
        //}

        //public void OnGet()
        //{

        //}

        private readonly IHttpContextAccessor _httpContextAccessor;

        // Свойства для отображения в View
        public string? CurrentUserName { get; private set; }
        public string? CurrentUserRole { get; private set; }
        public bool IsLoggedIn { get; private set; }

        public IndexModel(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnGet()
        {
            // Читаем данные из Session
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.Session != null)
            {
                CurrentUserName = httpContext.Session.GetString("UserName");
                CurrentUserRole = httpContext.Session.GetString("UserRole");
                IsLoggedIn = !string.IsNullOrEmpty(CurrentUserName);
            }
        }

        // ➕ Обработчик выхода из системы
        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear(); // Очищаем всю сессию
            return RedirectToPage("/Index");
        }


    }
}
