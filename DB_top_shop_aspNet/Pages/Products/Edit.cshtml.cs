using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DB_top_shop_aspNet.Pages.Products
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;
        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }


        [BindProperty]
        public Product Product { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            //var product = await _context.Products.FindAsync(id);
            //if (product == null) return NotFound();
            //Product = product;
            //return Page();


            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Попытка редактирования несуществующего продукта с ID {ProductId}.", id);
                return NotFound();
            }

            Product = product;
            return Page();





        }

        public async Task<IActionResult> OnPostAsync()
        {
            //if (!ModelState.IsValid) return Page();
            //_context.Attach(Product).State = EntityState.Modified;
            //try { await _context.SaveChangesAsync(); }
            //catch (DbUpdateConcurrencyException) { throw; }
            //return RedirectToPage("./Index");

            if (!ModelState.IsValid)
                return Page();

            try
            {
                _context.Attach(Product).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Продукт с ID {ProductId} (Наименование: {Name}) успешно обновлён.", Product.Id, Product.Name);
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {

                if (!ProductExists(Product.Id))
                {
                    _logger.LogWarning("Продукт с ID {ProductId} был удалён другим пользователем во время редактирования.", Product.Id);
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении продукта с ID {ProductId} (Наименование: {Name}).", Product.Id, Product.Name);
                ModelState.AddModelError(string.Empty, "Не удалось сохранить изменения. Повторите попытку.");
                return Page();
            }

        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }



    }
}
