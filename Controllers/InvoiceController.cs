using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoronelExpress.Data;

namespace CoronelExpress.Controllers
{
    // Con esta ruta, la URL será: /Invoice/GetInvoice
    [Route("Invoice")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Endpoint para POST /Invoice/GetInvoice
        [HttpPost("GetInvoice")]
        public IActionResult GetInvoice([FromBody] InvoiceRequest request)
        {
            try
            {
                // Validación básica: que se reciba un número de factura
                if (request == null || string.IsNullOrWhiteSpace(request.InvoiceNumber))
                {
                    return BadRequest(new { message = "El número de factura es requerido." });
                }

                // Limpiar el input para evitar espacios adicionales
                string invoiceNumber = request.InvoiceNumber.Trim();
                Console.WriteLine($"🔍 Buscando factura con OrderNumber: {invoiceNumber}");

                // Consulta utilizando LINQ con JOINs para relacionar Orders, Customers y PaymentMethods
                var invoice = (from o in _context.Orders
                               join c in _context.Customers on o.CustomerId equals c.Id into customers
                               from customer in customers.DefaultIfEmpty()
                               join p in _context.PaymentMethods on o.PaymentMethodId equals p.Id into payments
                               from payment in payments.DefaultIfEmpty()
                               where o.OrderNumber == invoiceNumber
                               select new
                               {
                                   OrderNumber = o.OrderNumber,
                                   OrderDate = o.OrderDate,
                                   TotalAmount = o.TotalAmount,
                                   Status = o.Status,
                                   // Se asigna "No disponible" si no se encuentra el cliente o el método de pago
                                   FullName = customer != null ? customer.FullName : "No disponible",
                                   Method = payment != null ? payment.Method : "No disponible"
                               }).FirstOrDefault();

                // Si no se encontró la factura, se retorna NotFound
                if (invoice == null)
                {
                    Console.WriteLine("⚠ Factura no encontrada.");
                    return NotFound(new { message = "Factura no encontrada." });
                }

                // Se muestran los datos en consola para depuración
                Console.WriteLine("✅ Factura encontrada:");
                Console.WriteLine($"   Número: {invoice.OrderNumber}");
                Console.WriteLine($"   Fecha: {invoice.OrderDate}");
                Console.WriteLine($"   Monto: {invoice.TotalAmount}");
                Console.WriteLine($"   Estado: {invoice.Status}");
                Console.WriteLine($"   Cliente: {invoice.FullName}");
                Console.WriteLine($"   Método de Pago: {invoice.Method}");

                // Se retorna la información en formato JSON
                return Ok(invoice);
            }
            catch (Exception ex)
            {
                // Se registra el error en consola para facilitar la depuración
                Console.WriteLine($"Error: {ex.Message}");
                // Se retorna un error 500 con mensaje; en producción evita exponer detalles internos
                return StatusCode(500, new { message = "Hubo un error al procesar la solicitud.", error = ex.Message });
            }
        }

        // Clase que representa el request recibido desde la vista
        public class InvoiceRequest
        {
            public string InvoiceNumber { get; set; }
        }
    }
}
