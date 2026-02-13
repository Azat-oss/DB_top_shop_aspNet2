using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DB_top_shop_aspNet.Data;
using DB_top_shop_aspNet.Models;

namespace DB_top_shop_aspNet.Pages.Orders
{
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
