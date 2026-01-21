using System;
using System.Linq;
using System.Threading.Tasks;
using CoronelExpress.Data;
using CoronelExpress.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoronelExpress.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acción que invoca la nueva vista "ReportesInteligentes"
        public async Task<IActionResult> ReportesInteligentes()
        {
            DateTime today = DateTime.Today;

            // Obtener las órdenes de hoy
            var ordersToday = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.OrderDate.Date == today)
                .ToListAsync();

            // Total de ventas del día
            decimal totalSales = ordersToday.Sum(o => o.TotalAmount);

            // Número de clientes únicos que compraron hoy
            int uniqueCustomers = ordersToday.Select(o => o.CustomerId).Distinct().Count();

            // Detalle de productos vendidos (agrupados por producto)
            var productsSold = await _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Order.OrderDate.Date == today)
                .GroupBy(od => od.Product.Name)
                .Select(g => new
                {
                    ProductName = g.Key,
                    TotalQuantity = g.Sum(od => od.Quantity)
                })
                .ToListAsync();

            // Total de cobranza (sólo transacciones cobradas)
            decimal totalCollected = await _context.PaymentTransactions
                .Where(pt => pt.TransactionDate.Date == today && pt.Status == "Cobrado")
                .SumAsync(pt => pt.Amount);

            // Control de pérdidas: ejemplo comparando total cobrado con un costo teórico
            decimal theoreticalCost = totalSales * 0.80m;
            decimal profitOrLoss = totalCollected - theoreticalCost;

            // Envío de datos a la vista (puedes utilizar un ViewModel si prefieres)
            ViewBag.TotalSales = totalSales;
            ViewBag.UniqueCustomers = uniqueCustomers;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.ProfitOrLoss = profitOrLoss;
            ViewBag.ProductsSold = productsSold;

            return View();
        }

        // Acción para reportes avanzados (buscador en tiempo real)
        public IActionResult AdvancedReports()
        {
            return View();
        }

        // Endpoint para datos filtrados vía AJAX (función del buscador en la vista de reportes avanzados)
        [HttpGet]
        public async Task<IActionResult> GetDailyReportData(DateTime? date, string transactionType = "Ventas")
        {
            DateTime targetDate = date?.Date ?? DateTime.Today;

            if (transactionType == "Ventas")
            {
                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Where(o => o.OrderDate.Date == targetDate)
                    .Select(o => new
                    {
                        o.OrderNumber,
                        Customer = o.Customer.FullName,
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status
                    })
                    .ToListAsync();

                return Json(new { success = true, data = orders });
            }
            else if (transactionType == "Cobranza")
            {
                var payments = await _context.PaymentTransactions
                    .Where(pt => pt.TransactionDate.Date == targetDate && pt.Status == "Cobrado")
                    .Select(pt => new
                    {
                        pt.OrderId,
                        pt.Amount,
                        pt.TransactionDate,
                        pt.PaymentMethod
                    })
                    .ToListAsync();

                return Json(new { success = true, data = payments });
            }
            return Json(new { success = false, message = "Tipo de transacción desconocido" });
        }
    }
}
