using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DB_top_shop_aspNet.Pages.Products
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;
        public DeleteModel(ApplicationDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;

        }

        [BindProperty]
        public Product Products { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Попытка удаления несуществующего продукта с ID {ProductId}.", id);
                return NotFound(); // лучше, чем исключение
            }
            Products = product;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            //var product = await _context.Products.FindAsync(id);
            //if (product != null)
            //{
            //    _context.Products.Remove(product);
            //    await _context.SaveChangesAsync();
            //}
            //return RedirectToPage("./Index");
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Продукт с ID {ProductId} уже удалён или не существует при попытке удаления.", id);
                    return RedirectToPage("./Index");
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Продукт с ID {ProductId} (Наименование: {Name}) успешно удалён.", product.Id, product.Name);
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении продукта с ID {ProductId}.", id);
                ModelState.AddModelError(string.Empty, "Не удалось удалить продукт. Повторите попытку.");
                return RedirectToPage("./Index");
            }




        }
    }
}
