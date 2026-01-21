using CoronelExpress.Data;
using Microsoft.AspNetCore.Mvc;

namespace CoronelExpress.Controllers
{
    public class OfertasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OfertasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acción para mostrar los productos en oferta
        public IActionResult Reales()
        {
            // Filtra los productos que están en oferta y tienen descuento
            var productosEnOferta = _context.Products
                .Where(p => p.IsOnOffer && p.DiscountPercentage > 0)
                .ToList();

            return View("OfertasReales", productosEnOferta);
        }
    }
}