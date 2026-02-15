using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DB_top_shop_aspNet.Pages.Products
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Product Product { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
           
            if (!ModelState.IsValid) return Page();

            try
            {
                _context.Products.Add(Product);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Продукт '{Name}' успешно создан.", Product.Name);

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании продукта '{Name}'.", Product.Name);
                ModelState.AddModelError(string.Empty, "Не удалось сохранить продукт. Проверьте данные и повторите попытку.");
                return Page();
            }
        }
    }
}
