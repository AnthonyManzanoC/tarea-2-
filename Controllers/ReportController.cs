using CoronelExpress.Data;
using Microsoft.AspNetCore.Mvc;

namespace CoronelExpress.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acción para mostrar el reporte de productos más vendidos
        public IActionResult BestSelling()
        {
            // Se agrupa OrderDetails por producto y se suma la cantidad vendida
            var reportData = _context.OrderDetails
                .GroupBy(od => od.Product)
                .Select(g => new BestSellingProductViewModel
                {
                    ProductId = g.Key.Id,
                    Name = g.Key.Name,
                    TotalSold = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .ToList();

            return View(reportData);
        }
    }

    // ViewModel para el reporte
    public class BestSellingProductViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int TotalSold { get; set; }
    }
}