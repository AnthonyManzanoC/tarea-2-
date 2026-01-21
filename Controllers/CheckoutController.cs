using CoronelExpress.Data;
using CoronelExpress.Helpers;
using CoronelExpress.Models;
using CoronelExpress.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace CoronelExpress.Controllers
{
    public class CheckoutController : Controller
    {
        private const string CartSessionKey = "cart";
        private readonly ApplicationDbContext _context;
        private readonly InventoryService _inventoryService;
        private const decimal ivaRate = 0.15m;

        public CheckoutController(ApplicationDbContext context, InventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        // GET: /Checkout/Index
        [HttpGet]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                // En GET se muestra el subtotal; en POST se calculará el total con IVA
                TotalAmount = cart.Sum(c => c.Product.Price * c.Quantity),
                PaymentMethods = _context.PaymentMethods.ToList()
            };

            return View(model);
        }

        // POST: /Checkout/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            // Validar modelo
            if (!ModelState.IsValid)
            {
                model.PaymentMethods = await _context.PaymentMethods.ToListAsync();
                model.CartItems = cart;
                model.TotalAmount = cart.Sum(c => c.Product.Price * c.Quantity);
                return View(model);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1️⃣ Registrar Cliente (incluyendo RUC/CI)
                    var customer = new Customer
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        Phone = model.Phone,
                        Address = model.Address,
                        RUC = model.RUC  // Se guarda el RUC/CI ingresado
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();

                    // Inicializamos variables para subtotal e IVA
                    decimal subTotal = 0;
                    decimal totalIva = 0;

                    // 2️⃣ Registrar Pedido con asignación de número de orden
                    var order = new Order
                    {
                        OrderDate = DateTime.Now,
                        CustomerId = customer.Id,
                        PaymentMethodId = model.PaymentMethodId,
                        OrderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                        OrderDetails = new List<OrderDetail>()
                    };

                    foreach (var item in cart)
                    {
                        var product = await _context.Products.FindAsync(item.Product.Id);
                        if (product == null || product.Stock < item.Quantity)
                        {
                            ModelState.AddModelError("", $"El producto {item.Product.Name} no tiene suficiente stock.");
                            model.PaymentMethods = await _context.PaymentMethods.ToListAsync();
                            return View(model);
                        }

                        // Actualizar stock y registrar movimiento "Salida" utilizando el servicio de inventario
                        await _inventoryService.UpdateStockAsync(product, item.Quantity, "Salida", "Venta en checkout");

                        // Calcular precio efectivo (aplica descuento si corresponde)
                        decimal effectivePrice = item.Product.Price;
                        if (product.IsOnOffer)
                        {
                            effectivePrice = effectivePrice * (1 - product.DiscountPercentage / 100);
                        }

                        decimal lineSubTotal = effectivePrice * item.Quantity;
                        subTotal += lineSubTotal;

                        if (!product.IsBasicBasket)
                        {
                            totalIva += lineSubTotal * ivaRate;
                        }

                        order.OrderDetails.Add(new OrderDetail
                        {
                            ProductId = item.Product.Id,
                            Quantity = item.Quantity,
                            UnitPrice = effectivePrice
                        });
                    }

                    order.TotalAmount = subTotal + totalIva;
                    order.Sequential = GenerateSequentialNumber();

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Enviar reporte de la transacción vía SMTP (incluye sección de productos sin stock si existieran)
                    try
                    {
                        var outOfStockProducts = _context.Products
                            .Where(p => p.Stock == 0 && !p.NotifiedOutOfStock)
                            .ToList();

                        // Se envía el correo siempre, adjuntando la sección de productos sin stock solo si hay alguno.
                        SendOutOfStockNotification(outOfStockProducts, order, customer);

                        if (outOfStockProducts.Any())
                        {
                            foreach (var product in outOfStockProducts)
                            {
                                product.NotifiedOutOfStock = true;
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Se podría registrar el error sin interrumpir el flujo normal del proceso.
                    }

                    // Limpiar el carrito
                    HttpContext.Session.Remove(CartSessionKey);

                    return RedirectToAction("Confirmation", new { orderId = order.Id });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Ocurrió un error al procesar el pedido.");
                    model.PaymentMethods = await _context.PaymentMethods.ToListAsync();
                    return View(model);
                }
            }
        }

        // GET: /Checkout/Confirmation?orderId=123
        [HttpGet]
        public async Task<IActionResult> Confirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // La vista Invoice mostrará el RUC/CI ingresado y el número secuencial generado.
            return View(order);
        }

        // Método para generar un número secuencial para la factura.
        private string GenerateSequentialNumber()
        {
            var lastOrder = _context.Orders.OrderByDescending(o => o.Id).FirstOrDefault();
            if (lastOrder != null && !string.IsNullOrEmpty(lastOrder.Sequential))
            {
                if (int.TryParse(lastOrder.Sequential, out int lastNumber))
                {
                    return (lastNumber + 1).ToString("D9"); // 9 dígitos con ceros a la izquierda
                }
            }
            return "000000001";
        }

        // Método para enviar notificación de la transacción vía SMTP (correo en HTML).
        // Si existen productos sin stock, se incluirá una sección adicional con ellos.
        private void SendOutOfStockNotification(List<Product> products, Order order, Customer customer)
        {
            string outOfStockSection = "";
            if (products.Any())
            {
                outOfStockSection = @"<div class='section'>
      <h2>Productos sin Stock</h2>
      <table>
        <tr>
          <th>ID</th>
          <th>Nombre</th>
        </tr>";
                foreach (var product in products)
                {
                    outOfStockSection += "<tr><td>" + product.Id + "</td><td>" + product.Name + "</td></tr>";
                }
                outOfStockSection += @"</table>
    </div>";
            }

            string body = @"<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body { font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }
    .container { background-color: #ffffff; padding: 20px; border-radius: 5px; box-shadow: 0 2px 3px rgba(0,0,0,0.1); }
    .header { font-size: 24px; font-weight: bold; color: #333333; margin-bottom: 20px; }
    .section { margin-bottom: 20px; }
    .section h2 { font-size: 20px; color: #555555; margin-bottom: 10px; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 8px 12px; border: 1px solid #dddddd; text-align: left; }
    th { background-color: #f0f0f0; }
    .footer { font-size: 12px; color: #777777; text-align: center; margin-top: 20px; }
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>Reporte Corporativo de Transacción</div>
    <div class='section'>
      <h2>Detalles de la Transacción</h2>
      <p><strong>Número de Orden:</strong> " + order.OrderNumber + @"</p>
      <p><strong>Fecha:</strong> " + order.OrderDate.ToString("dd/MM/yyyy HH:mm") + @"</p>
      <p><strong>Total:</strong> $" + order.TotalAmount.ToString("F2") + @"</p>
      <p><strong>Cliente:</strong> " + customer.FullName + @"</p>
    </div>
    " + outOfStockSection + @"
    <div class='section'>
      <p>Esta notificación fue generada automáticamente. Por favor, revise la bodega para reabastecer el stock de los productos.</p>
    </div>
    <div class='footer'>
      &copy; " + DateTime.Now.Year + "  - Don Julio Super  Plataforma Generada por Coronel Express. Todos los derechos reservados." +
                @"</div>
  </div>
</body>
</html>";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("tu_correo@dominio.com");  // Reemplazar con el correo corporativo adecuado
            mail.To.Add("manzanocoroneljulioanthony@gmail.com");     // Correo de administración
            mail.Subject = "Reporte: Transacción Realizada" + (products.Any() ? " y Productos sin Stock" : "");
            mail.Body = body;
            mail.IsBodyHtml = true;

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
            smtpClient.EnableSsl = true;

            smtpClient.Send(mail);
        }
    }
}

