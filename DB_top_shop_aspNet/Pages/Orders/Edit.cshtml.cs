using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;

namespace DB_top_shop_aspNet.Pages.Orders
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public Order Order { get; set; } = new();

        public SelectList ClientsSelectList { get; set; } = new SelectList(new List<Client>(), "Id", "Name");
        public SelectList ProductsSelectList { get; set; } = new SelectList(new List<Product>(), "Id", "Name");

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Загружаем заказ из базы
            var order = await _context.Orders.FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            Order = order;

            // 2. Загружаем списки для выпадающих меню
            var clients = await _context.Clients.ToListAsync();
            var products = await _context.Products.ToListAsync();

            // ВАЖНО: Указываем текущее выбранное значение (order.ClientId), 
            // чтобы в списке был выделен нужный клиент/продукт
            ClientsSelectList = new SelectList(clients, "Id", "Name", order.ClientId);
            ProductsSelectList = new SelectList(products, "Id", "Name", order.ProductId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 🔧 1. Удаляем навигационные свойства из проверки валидации
            // (чтобы не было ошибок типа "Поле Client обязательно", когда мы отправили только ClientId)
            ModelState.Remove("Order.Client");
            ModelState.Remove("Order.Product");

            // 2. Проверяем валидацию
            if (!ModelState.IsValid)
            {
                // ❗ Если здесь не перезагрузить списки, страница упадет с ошибкой при рендеринге!
                var clients = await _context.Clients.ToListAsync();
                var products = await _context.Products.ToListAsync();

                // Восстанавливаем выбор пользователя (Order.ClientId содержит то, что выбрал юзер в форме)
                ClientsSelectList = new SelectList(clients, "Id", "Name", Order.ClientId);
                ProductsSelectList = new SelectList(products, "Id", "Name", Order.ProductId);

                return Page(); // Возвращаем форму с ошибками
            }

            // 3. Сохраняем изменения
            _context.Attach(Order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(Order.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
