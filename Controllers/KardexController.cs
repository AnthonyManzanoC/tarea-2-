using CoronelExpress.Data;
using CoronelExpress.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace CoronelExpress.Controllers
{
    
    public class KardexController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KardexController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Muestra el control Kardex con los productos y sus movimientos
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.Include(p => p.Category).ToListAsync();
            return View(products);
        }

        // Muestra el detalle de movimientos de un producto
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var movements = await _context.InventoryMovement
                .Where(m => m.ProductId == id)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            ViewBag.Product = product;
            return View(movements);
        }

        // Formulario para registrar una entrada/salida de stock
        public async Task<IActionResult> RegisterMovement(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(new InventoryMovement { ProductId = id });
        }
        [HttpGet("GetStock/{productId}")]
        public async Task<IActionResult> GetStock(int productId)
        {
            try
            {
                Console.WriteLine($"🔍 Buscando stock para el producto ID: {productId}");

                var product = await _context.Products.FindAsync(productId);

                if (product == null)
                {
                    Console.WriteLine($"❌ Producto con ID {productId} no encontrado.");
                    return NotFound(new { message = "Producto no encontrado." });
                }

                Console.WriteLine($"✅ Producto encontrado, stock actual: {product.Stock}");
                return Ok(new { stock = product.Stock });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener el stock: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterMovement(InventoryMovement movement)
        {
            // Forzar Id a 0 para que la base de datos lo genere automáticamente
            movement.Id = 0;

            var product = await _context.Products.FindAsync(movement.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            // Si el movimiento es de "Salida", se valida que haya stock suficiente.
            if (movement.Type == "Salida")
            {
                if (movement.Quantity > product.Stock)
                {
                    ModelState.AddModelError("", "No hay suficiente stock disponible.");
                    return View(movement);
                }
            }
            // Si es de "Devolucion", se evita que la cantidad supere el stock actual
            // (para que no se reste de más y se marque en negativo)
            else if (movement.Type == "Devolucion")
            {
                if (movement.Quantity > product.Stock)
                {
                    // Si se intenta devolver más de lo que hay en stock, se ajusta la cantidad
                    movement.Quantity = product.Stock;
                }
            }

            movement.Date = DateTime.Now;
            _context.InventoryMovement.Add(movement);

            // Actualizar stock según el tipo de movimiento:
            // Para "Entrada" se suma; para "Salida" o "Devolucion" se resta.
            if (movement.Type == "Entrada")
            {
                product.Stock += movement.Quantity;
            }
            else // "Salida" o "Devolucion"
            {
                product.Stock -= movement.Quantity;
            }

            _context.Products.Update(product);

            try
            {
                await _context.SaveChangesAsync();

                // Si es una devolución y tras la operación el stock queda en 0,
                // se envía un correo de notificación y se marca el producto como notificado.
                if (movement.Type == "Devolucion" && product.Stock == 0)
                {
                    SendEmptyStockNotification(product, movement);
                    product.NotifiedOutOfStock = true;
                    _context.Products.Update(product);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al guardar el movimiento: " + ex.Message);
                return View(movement);
            }

            return RedirectToAction(nameof(Index));
        }


        private void SendEmptyStockNotification(Product product, InventoryMovement movement)
        {
            string body = @"<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body { font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; }
    .container { background-color: #ffffff; padding: 20px; border-radius: 5px; }
    .header { font-size: 24px; font-weight: bold; margin-bottom: 20px; }
    .section { margin-bottom: 20px; }
    .footer { font-size: 12px; color: #777777; text-align: center; margin-top: 20px; }
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>Alerta: Devolución vació el stock</div>
    <div class='section'>
      <p>El movimiento de devolución ha dejado sin stock el siguiente producto:</p>
      <p><strong>ID:</strong> " + product.Id + @"</p>
      <p><strong>Nombre:</strong> " + product.Name + @"</p>
      <p><strong>Fecha del Movimiento:</strong> " + movement.Date.ToString("dd/MM/yyyy HH:mm") + @"</p>
      <p><strong>Cantidad devuelta:</strong> " + movement.Quantity + @"</p>
    </div>
    <div class='footer'>
      &copy; " + DateTime.Now.Year + @" Coronel Express. Todos los derechos reservados.
    </div>
  </div>
</body>
</html>";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("tu_correo@dominio.com");  // Reemplazar con el correo corporativo adecuado
            mail.To.Add("manzanocoroneljulioanthony@gmail.com");     // Correo de administración
            mail.Subject = "Alerta: Producto sin Stock tras Devolución";
            mail.Body = body;
            mail.IsBodyHtml = true;

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
            smtpClient.EnableSsl = true;

            smtpClient.Send(mail);
        }
        public class RegisterMovementViewModel
        {
            public int ProductId { get; set; }
            public int Stock { get; set; }
        }
    }
}
