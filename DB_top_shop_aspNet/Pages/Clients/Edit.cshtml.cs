using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DB_top_shop_aspNet.Pages.Clients
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
        public Client Client { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                _logger.LogWarning("Попытка редактирования несуществующего клиента с ID {ClientId}.", id);
                return NotFound();
            }

            Client = client;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            
            if (!ModelState.IsValid)
                return Page();

            try
            {
                _context.Attach(Client).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Клиент с ID {ClientId} (email: {Email}) успешно обновлён.", Client.Id, Client.Email);
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
               
                if (!ClientExists(Client.Id))
                {
                    _logger.LogWarning("Клиент с ID {ClientId} был удалён другим пользователем во время редактирования.", Client.Id);
                    return NotFound();
                }
                else
                {
                    throw; 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении клиента с ID {ClientId} (email: {Email}).", Client.Id, Client.Email);
                ModelState.AddModelError(string.Empty, "Не удалось сохранить изменения. Повторите попытку.");
                return Page();
            }

        }
        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }
}
