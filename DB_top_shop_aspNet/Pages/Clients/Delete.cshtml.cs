using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DB_top_shop_aspNet.Pages.Clients
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
        public Client Client { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                _logger.LogWarning("Попытка удаления несуществующего клиента с ID {ClientId}.", id);
                return NotFound(); 
            }
            Client = client;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var client = await _context.Clients.FindAsync(id);
                if (client == null)
                {
                    _logger.LogWarning("Клиент с ID {ClientId} уже удалён или не существует при попытке удаления.", id);
                    return RedirectToPage("./Index");
                }

                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Клиент с ID {ClientId} (Имя: {Name}) успешно удалён.", client.Id, client.Name);
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении клиента с ID {ClientId}.", id);
                ModelState.AddModelError(string.Empty, "Не удалось удалить клиента. Повторите попытку.");
                return RedirectToPage("./Index"); 
            }
        }
    }
}
