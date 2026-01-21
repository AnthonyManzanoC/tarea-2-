using CoronelExpress.Data;
using Microsoft.AspNetCore.Mvc;
using CoronelExpress.Models;

namespace CoronelExpress.Controllers
{
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acción que muestra los productos sin stock
        public IActionResult Index()
        {
            var outOfStockProducts = _context.Products
                .Where(p => p.Stock == 0)
                .ToList();

            return View(outOfStockProducts);
        }
    }
}
