using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DB_top_shop_aspNet.Pages.Orders
{
    [Authorize(Policy = "ManagerOrAdmin")] // Менеджер видит, Админ делает всё
    public class IndexModel : PageModel
    {
        private readonly DB_top_shop_aspNet.Data.ApplicationDbContext _context;

        public IndexModel(DB_top_shop_aspNet.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Order> Order { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Product).ToListAsync();
        }
    }
}
