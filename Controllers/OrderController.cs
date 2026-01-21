using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoronelExpress.Data;
using CoronelExpress.Models;
using iTextSharp.text.pdf.draw;
using CoronelExpress.Services;
using System.Text;

namespace CoronelExpress.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly InventoryService _inventoryService; // Agrega esta línea
        private const decimal ivaRate = 0.15m;
        private readonly IPaymentService _paymentService;

        public OrderController(ApplicationDbContext context, InventoryService inventoryService, IPaymentService paymentService) // Modifica el constructor
        {
            _context = context;
            _inventoryService = inventoryService; // Asigna el servicio
            _paymentService = paymentService;

        }

        public async Task<IActionResult> Invoice(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            return View("~/Views/Order/Invoice.cshtml", order);
        }

        public async Task<IActionResult> GenerateInvoice(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)

                    .ThenInclude(od => od.Product)
                .Include(o => o.PaymentMethod) // Carga el método de pago

                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            byte[] pdfBytes = GenerateInvoicePdf(order);
            return File(pdfBytes, "application/pdf", $"Factura_{order.OrderNumber}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> SendInvoiceEmailForm(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> SendInvoiceEmail(int orderId, string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "El correo electrónico es obligatorio.";
                return RedirectToAction("SendInvoiceEmailForm", new { orderId });
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.PaymentMethod) // Carga el método de pago
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Orden no encontrada.";
                return RedirectToAction("SendInvoiceEmailForm", new { orderId });
            }

            try
            {
                byte[] pdfBytes = GenerateInvoicePdf(order);

                MailMessage mail = new MailMessage
                {
                    From = new MailAddress("comercialdonjulio@example.com", "Comercial Don Julio"),
                    Subject = "Factura del sistema DonJulio Súper",
                    IsBodyHtml = true
                };
                mail.To.Add(email);

                // Construir el cuerpo del correo con diseño corporativo avanzado
                var body = new StringBuilder();
                body.AppendLine("<!DOCTYPE html>");
                body.AppendLine("<html lang='es'>");
                body.AppendLine("<head>");
                body.AppendLine("    <meta charset='UTF-8'>");
                body.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                body.AppendLine("    <title>Factura Electrónica</title>");
                body.AppendLine("    <style>");
                body.AppendLine("        body { margin: 0; padding: 0; background-color: #f4f4f4; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }");
                body.AppendLine("        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border: 1px solid #e0e0e0; }");
                body.AppendLine("        .header { background-color: #0056b3; padding: 20px; text-align: center; }");
                body.AppendLine("        .header h1 { margin: 0; color: #ffffff; font-size: 24px; }");
                body.AppendLine("        .content { padding: 20px; }");
                body.AppendLine("        .content p { font-size: 16px; line-height: 1.6; color: #333333; }");
                body.AppendLine("        .footer { background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 12px; color: #777777; border-top: 1px solid #e0e0e0; }");
                body.AppendLine("    </style>");
                body.AppendLine("</head>");
                body.AppendLine("<body>");
                body.AppendLine("    <div class='container'>");
                body.AppendLine("        <div class='header'>");
                body.AppendLine("            <h1>DonJulio Súper</h1>");
                body.AppendLine("        </div>");
                body.AppendLine("        <div class='content'>");
                body.AppendLine("            <p>Estimado cliente,</p>");
                body.AppendLine("            <p>Adjunto encontrará la factura de su pedido. Agradecemos su compra en DonJulio Súper.</p>");
                body.AppendLine("        </div>");
                body.AppendLine("        <div class='footer'>");
                body.AppendLine("            <p>Generado automáticamente por el envío de la factura electrónica.</p>");
                body.AppendLine("            <p>&copy; " + DateTime.Now.Year + " Comercial Don Julio. Todos los derechos reservados.</p>");
                body.AppendLine("        </div>");
                body.AppendLine("    </div>");
                body.AppendLine("</body>");
                body.AppendLine("</html>");

                mail.Body = body.ToString();

                using (MemoryStream attachmentStream = new MemoryStream(pdfBytes))
                {
                    mail.Attachments.Add(new Attachment(attachmentStream, $"Factura_{order.OrderNumber}.pdf", "application/pdf"));
                    using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
                    {
                        client.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                        client.EnableSsl = true;
                        await client.SendMailAsync(mail);
                    }
                }

                await GuardarEmailEnOrden(order, email);

                TempData["Success"] = "Factura enviada con éxito.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al enviar la factura: " + ex.Message;
            }

            return RedirectToAction("SendInvoiceEmailForm", new { orderId });
        }

        [HttpGet]
        public async Task<IActionResult> ProcessOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(int orderId, string? processCode)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Orden no encontrada.";
                return RedirectToAction("SearchOrders");
            }

            // -- Validaciones para no reprocesar/cancelar/completar --
            if (order.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "No se puede procesar una orden que está cancelada.";
                return RedirectToAction("ProcessOrder", new { orderId = order.Id });
            }

            if (order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "La orden ya fue completada y no se puede procesar de nuevo.";
                return RedirectToAction("ProcessOrder", new { orderId = order.Id });
            }

            if (order.Status.Equals("En Proceso", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "La orden ya está en proceso.";
                return RedirectToAction("ProcessOrder", new { orderId = order.Id });
            }

            // -- Lógica original para procesar la orden --
            if (string.IsNullOrWhiteSpace(processCode))
            {
                var random = new Random();
                processCode = random.Next(100000, 1000000).ToString();
            }

            order.DeliveryConfirmationCode = processCode;
            order.Status = "En Proceso";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            string emailResult = await SendEmailNotification(
                order.InvoiceEmail,
                order.Customer.FullName,
                processCode,
                order.Id
            );

            if (emailResult == "OK")
            {
                TempData["Success"] = "Pedido actualizado a 'En Proceso' y notificación enviada.";
            }
            else
            {
                TempData["Error"] = $"Pedido actualizado, pero hubo un error al enviar el correo: {emailResult}";
            }

            return RedirectToAction("ProcessOrder", new { orderId = order.Id });
        }
       


        [HttpGet]
        public async Task<IActionResult> ConfirmDelivery(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            if (order.Status != "En Proceso")
            {
                TempData["Error"] = "El pedido no está en proceso.";
                return RedirectToAction("OrderDetails", new { orderId = order.Id });
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmDelivery(int orderId, string enteredCode)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Orden no encontrada.";
                return RedirectToAction("SearchOrders");
            }

            if (order.DeliveryConfirmationCode == enteredCode)
            {
                order.Status = "Completo";
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                // Validar que el método de pago esté presente y sea uno válido para registrar el pago
                if (order.PaymentMethod != null &&
                    !string.IsNullOrEmpty(order.PaymentMethod.Method) &&
                    new[] { "contraentrega", "efectivo", "cheque", "transferencia" }
                        .Contains(order.PaymentMethod.Method.ToLower()))
                {
                    await _paymentService.RegisterPaymentAsync(order);
                }

                TempData["Success"] = "La entrega se ha confirmado y el pago se ha registrado correctamente.";
                return RedirectToAction("ThankYou", new { orderId = order.Id });
            }
            else
            {
                TempData["Error"] = "El código ingresado es incorrecto. Por favor, intente nuevamente.";
                return RedirectToAction("ConfirmDelivery", new { orderId = order.Id });
            }
        }



        [HttpGet]
        public IActionResult ThankYou(int orderId)
        {
            ViewData["OrderId"] = orderId;
            return View();
        }

        private async Task<string> SendEmailNotification(string email, string customerName, string processCode, int orderId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "El correo del cliente no está configurado.";

            try
            {
                string confirmDeliveryUrl = Url.Action("ConfirmDelivery", "Order", new { orderId = orderId }, Request.Scheme);

                MailMessage mail = new MailMessage
                {
                    From = new MailAddress("comercialdonjulio@example.com", "Comercial Don Julio"),
                    Subject = "Su pedido está en proceso",
                    Body = $@"
        <p>Estimado/a {customerName},</p>
        <p>Su pedido se ha puesto en estado <strong>En Proceso</strong>.</p>
        <p>Su código de confirmación es: <strong>{processCode}</strong>.</p>
        <p>Puede confirmar la entrega accediendo al siguiente enlace:</p>
        <p><a href='{confirmDeliveryUrl}'>Confirmar Entrega</a></p>
        <p>Gracias por confiar en nosotros.</p>",
                    IsBodyHtml = true
                };
                mail.To.Add(email);

                using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                    client.EnableSsl = true;
                    await client.SendMailAsync(mail);
                }

                return "OK";
            }
            catch (SmtpException smtpEx)
            {
                return $"Error SMTP: {smtpEx.Message}";
            }
            catch (Exception ex)
            {
                return $"Error general: {ex.Message}";
            }
        }

        private async Task GuardarEmailEnOrden(Order order, string email)
        {
            order.InvoiceEmail = email;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        public async Task<IActionResult> FinalizeTransaction(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            return View("FinalizedInvoice", order);
        }

        [HttpGet]
        public async Task<IActionResult> FinalizedInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)


                    .ThenInclude(od => od.Product)

                .FirstOrDefaultAsync(o => o.Id == id)
                ;

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
       

        [HttpPost]
        public async Task<IActionResult> CancelTransaction(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // 1. Revertir el stock de cada producto utilizando el servicio de inventario
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product != null)
                {
                    // Se registra un movimiento "Entrada" para revertir la salida realizada al vender
                    await _inventoryService.UpdateStockAsync(detail.Product, detail.Quantity, "Entrada", "Reversión cancelación de pedido");
                }
            }

            // 2. Marcar la orden como cancelada
            order.Status = "Cancelled";
            _context.Orders.Update(order);

            // 3. Guardar cambios
            await _context.SaveChangesAsync();

            // 4. Enviar reporte corporativo por correo (SMTP) con los detalles de la cancelación

            // Construir sección de productos cancelados (tabla con detalles de cada producto)
            string cancelledItemsSection = "";
            if (order.OrderDetails.Any())
            {
                cancelledItemsSection = "<div class='section'><h2>Productos Cancelados</h2><table><tr><th>ID</th><th>Nombre</th><th>Cantidad</th></tr>";
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Product != null)
                    {
                        cancelledItemsSection += $"<tr><td>{detail.Product.Id}</td><td>{detail.Product.Name}</td><td>{detail.Quantity}</td></tr>";
                    }
                }
                cancelledItemsSection += "</table></div>";
            }

            // Construir el cuerpo del correo usando el template corporativo
            string body = @"
<html>
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
    <div class='header'>Reporte Corporativo de Cancelación de Transacción</div>
    <div class='section'>
      <h2>Detalles de la Transacción Cancelada</h2>
      <p><strong>Número de Orden:</strong> " + order.OrderNumber + @"</p>
      <p><strong>Fecha:</strong> " + order.OrderDate.ToString("dd/MM/yyyy HH:mm") + @"</p>
      <p><strong>Total:</strong> $" + order.TotalAmount.ToString("F2") + @"</p>
      <p><strong>Cliente:</strong> " + order.Customer.FullName + @"</p>
    </div>
    " + cancelledItemsSection + @"
    <div class='section'>
      <p>Esta notificación fue generada automáticamente debido a la cancelación de la transacción.</p>
    </div>
    <div class='footer'>
      &copy; " + DateTime.Now.Year + "  - Don Julio Super  Plataforma Generada por Coronel Express. Todos los derechos reservados." +
                        @"</div>
  </div>
</body>
</html>";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("tu_correo@dominio.com"); // Reemplaza con el correo corporativo adecuado
            mail.To.Add("manzanocoroneljulioanthony@gmail.com");   // Correo de administración
            mail.Subject = "Reporte Corporativo: Cancelación de Transacción - Orden " + order.OrderNumber;
            mail.Body = body;
            mail.IsBodyHtml = true;

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
            smtpClient.EnableSsl = true;

            smtpClient.Send(mail);

            // 5. Redirigir a la página principal o la que desees
            return RedirectToAction("Index", "Home");
        }


        private byte[] GenerateInvoicePdf(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order), "La orden no puede ser nula.");
            }

            // Inicialización de variables para el cálculo de totales
            decimal subTotal = 0;
            decimal totalIva = 0;
            decimal ivaRate = 0.15m;

            if (order.OrderDetails != null && order.OrderDetails.Count > 0)
            {
                foreach (var detail in order.OrderDetails)
                {
                    decimal originalPrice = (detail.Product != null) ? detail.Product.Price : detail.UnitPrice;
                    decimal discountPercentage = (detail.Product != null && detail.Product.IsOnOffer) ? detail.Product.DiscountPercentage : 0;
                    decimal effectivePrice = (discountPercentage > 0) ? originalPrice * (1 - discountPercentage / 100m) : originalPrice;
                    decimal lineSubTotal = effectivePrice * detail.Quantity;
                    subTotal += lineSubTotal;

                    if (detail.Product != null && !detail.Product.IsBasicBasket)
                    {
                        totalIva += lineSubTotal * ivaRate;
                    }
                }
            }

            using (var workStream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 50, 50, 80, 50);
                PdfWriter writer = PdfWriter.GetInstance(document, workStream);
                writer.CloseStream = false;
                document.Open();

                // ================================================================
                // Definición de fuentes y colores de lujo
                // ================================================================
                BaseColor primaryColor = new BaseColor(0, 51, 102);           // Azul oscuro
                BaseColor secondaryColor = new BaseColor(15, 76, 129);          // Azul corporativo intenso
                BaseColor accentColor = new BaseColor(0, 102, 204);            // Azul brillante para acentos
                BaseColor headerBg = new BaseColor(245, 245, 245);          // Fondo claro para paneles
                BaseColor panelBorder = new BaseColor(200, 200, 200);          // Bordes sutiles
                Font headerTitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.WHITE);
                Font headerSubFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.WHITE);
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22, primaryColor);
                Font panelTitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, primaryColor);
                Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
                Font regularFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.DARK_GRAY);
                Font tableHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                Font footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);

                // ================================================================
                // Encabezado premium: Bloque sólido, logo e ícono de inicio
                // ================================================================
                PdfPTable headerTable = new PdfPTable(2) { WidthPercentage = 100 };
                headerTable.SetWidths(new float[] { 1, 3 });
                PdfPCell headerCell = new PdfPCell
                {
                    Colspan = 2,
                    Border = PdfPCell.NO_BORDER,
                    Padding = 10,
                    BackgroundColor = secondaryColor
                };
                PdfPTable innerHeaderTable = new PdfPTable(2) { WidthPercentage = 100 };
                innerHeaderTable.SetWidths(new float[] { 1, 3 });

                try
                {
                    // Logo corporativo
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.png");
                    iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(imagePath);
                    logo.ScaleToFit(70, 70);
                    PdfPCell logoCell = new PdfPCell(logo)
                    {
                        Border = PdfPCell.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5
                    };
                    innerHeaderTable.AddCell(logoCell);
                }
                catch
                {
                    PdfPCell noLogoCell = new PdfPCell(new Phrase("logo.png", regularFont))
                    {
                        Border = PdfPCell.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5
                    };
                    innerHeaderTable.AddCell(noLogoCell);
                }
                PdfPCell companyCell = new PdfPCell { Border = PdfPCell.NO_BORDER, Padding = 5 };
                companyCell.AddElement(new Paragraph("DonJulio Super", headerTitleFont));
                companyCell.AddElement(new Paragraph("Montalvo Los Ríos, Ecuador", headerSubFont));
                innerHeaderTable.AddCell(companyCell);
                headerCell.AddElement(innerHeaderTable);
                headerTable.AddCell(headerCell);
                document.Add(headerTable);

                // Separador con efecto lujoso
                LineSeparator headerLine = new LineSeparator(2f, 100, primaryColor, Element.ALIGN_CENTER, -2);
                document.Add(new Chunk(headerLine));

                // ================================================================
                // Título principal y mensaje de éxito (con íconos decorativos)
                // ================================================================
                Paragraph invoiceTitle = new Paragraph("✦ Factura Electrónica ✦", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 20,
                    SpacingAfter = 5
                };
                document.Add(invoiceTitle);
                Paragraph successMessage = new Paragraph("✔ Transacción exitosa: ¡Compra realizada con éxito!", regularFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15
                };
                document.Add(successMessage);

                // ================================================================
                // Paneles corporativos ultra premium: Información de Factura, Emisor y Comprador
                // ================================================================
                PdfPTable panelTable = new PdfPTable(3) { WidthPercentage = 100, SpacingBefore = 10, SpacingAfter = 20 };
                panelTable.SetWidths(new float[] { 1, 1, 1 });

                // Función local para crear paneles con íconos, sombra y borde refinado
                PdfPTable CreatePanel(string title, Dictionary<string, string> data, string iconPath = null)
                {
                    PdfPTable panel = new PdfPTable(1) { WidthPercentage = 100 };
                    // Encabezado del panel con ícono (si existe) y fondo claro
                    Phrase titlePhrase = new Phrase();
                    if (!string.IsNullOrEmpty(iconPath))
                    {
                        try
                        {
                            iTextSharp.text.Image icon = iTextSharp.text.Image.GetInstance(iconPath);
                            icon.ScaleAbsolute(12, 12);
                            Chunk iconChunk = new Chunk(icon, 0, -2);
                            titlePhrase.Add(iconChunk);
                            titlePhrase.Add(" ");
                        }
                        catch { }
                    }
                    titlePhrase.Add(new Chunk(title, panelTitleFont));
                    PdfPCell titleCell = new PdfPCell(titlePhrase)
                    {
                        BackgroundColor = headerBg,
                        BorderWidth = 1,
                        BorderColor = panelBorder,
                        Padding = 5,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    };
                    panel.AddCell(titleCell);
                    // Datos en formato tabla
                    PdfPTable dataTable = new PdfPTable(2) { WidthPercentage = 100 };
                    dataTable.SetWidths(new float[] { 1, 1 });
                    foreach (var kvp in data)
                    {
                        dataTable.AddCell(new PdfPCell(new Phrase(kvp.Key, boldFont))
                        {
                            Border = PdfPCell.NO_BORDER,
                            Padding = 3
                        });
                        dataTable.AddCell(new PdfPCell(new Phrase(kvp.Value, regularFont))
                        {
                            Border = PdfPCell.NO_BORDER,
                            Padding = 3
                        });
                    }
                    PdfPCell dataCell = new PdfPCell(dataTable)
                    {
                        Border = PdfPCell.NO_BORDER,
                        Padding = 5
                    };
                    panel.AddCell(dataCell);
                    return panel;
                }

                // Panel: Información de la Factura (con ícono representativo)
                var invoiceData = new Dictionary<string, string>
        {
            { "Nº de Factura:", order.OrderNumber?.ToString() ?? "N/A" },
            { "Fecha de Emisión:", order.OrderDate.ToString("dd/MM/yyyy HH:mm") },
            { "Secuencial:", order.Sequential?.ToString() ?? "N/A" },
            { "Clave de Acceso:", "12345678910" },
          
            { "Nº de Autorización:", "11122333456788999" },
            { "F. Autorización:", "Enero de 2001" }
        };
                PdfPTable invoicePanel = CreatePanel("Información de la Factura", invoiceData, "wwwroot/images/info.png");

                // Panel: Datos del Emisor
                var emitterData = new Dictionary<string, string>
        {
            { "Razón Social:", "Julio Manzano C" },
            { "RUC:", "1201783998001" },
            { "Nombre Comercial:", "Comercial Don Julio" },
            { "Dirección Matriz:", "Dirección de la Empresa" },
            { "Ambiente:", "Produccion" },
            { "Contabilidad:", "Sí" }
        };
                PdfPTable emitterPanel = CreatePanel("Datos del Emisor", emitterData, "wwwroot/images/seller.png");

                // Panel: Datos del Comprador
                var buyerData = new Dictionary<string, string>
        {
            { "Nombre:", order.Customer?.FullName ?? "Cliente no especificado" },
            { "RUC/CI:", order.Customer?.RUC ?? "N/A" },
            { "Email:", order.Customer?.Email ?? "N/A" },
            { "Teléfono:", order.Customer?.Phone ?? "N/A" },
            { "Dirección:", order.Customer?.Address ?? "N/A" },
{ "Método de pago (Contra entrega):", order.PaymentMethod != null ? order.PaymentMethod.Method : "No especificado" }

        };
                PdfPTable buyerPanel = CreatePanel("Datos del Comprador", buyerData, "wwwroot/images/buyer.png");

                PdfPCell invoicePanelCell = new PdfPCell(invoicePanel) { Border = PdfPCell.NO_BORDER, Padding = 5 };
                PdfPCell emitterPanelCell = new PdfPCell(emitterPanel) { Border = PdfPCell.NO_BORDER, Padding = 5 };
                PdfPCell buyerPanelCell = new PdfPCell(buyerPanel) { Border = PdfPCell.NO_BORDER, Padding = 5 };
                panelTable.AddCell(invoicePanelCell);
                panelTable.AddCell(emitterPanelCell);
                panelTable.AddCell(buyerPanelCell);
                document.Add(panelTable);

                // ================================================================
                // Detalles del Pedido: Tabla de Productos con diseño minimalista y refinado
                // ================================================================
                Paragraph detailsTitle = new Paragraph("Detalles del Pedido", boldFont)
                {
                    Alignment = Element.ALIGN_LEFT,
                    SpacingBefore = 10,
                    SpacingAfter = 5
                };
                document.Add(detailsTitle);
                PdfPTable productTable = new PdfPTable(6) { WidthPercentage = 100, SpacingBefore = 5, SpacingAfter = 10 };
                productTable.SetWidths(new float[] { 4, 1, 2, 1, 2, 2 });

                // Encabezado de la tabla con fondo de acento
                productTable.AddCell(new PdfPCell(new Phrase("Producto", tableHeaderFont))
                {
                    BackgroundColor = accentColor,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                });
                productTable.AddCell(new PdfPCell(new Phrase("Cant.", tableHeaderFont))
                {
                    BackgroundColor = accentColor,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                });
                productTable.AddCell(new PdfPCell(new Phrase("Precio Orig.", tableHeaderFont))
                {
                    BackgroundColor = accentColor,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                });
                productTable.AddCell(new PdfPCell(new Phrase("Desc. (%)", tableHeaderFont))
                {
                    BackgroundColor = accentColor,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                });
                productTable.AddCell(new PdfPCell(new Phrase("Precio Final", tableHeaderFont))
                {
                    BackgroundColor = accentColor,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                });
                productTable.AddCell(new PdfPCell(new Phrase("Total", tableHeaderFont))
                {
                    BackgroundColor = accentColor,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                });

                if (order.OrderDetails != null && order.OrderDetails.Count > 0)
                {
                    int rowCount = 0;
                    foreach (var detail in order.OrderDetails)
                    {
                        rowCount++;
                        BaseColor rowBackground = (rowCount % 2 == 0) ? new BaseColor(245, 245, 245) : BaseColor.WHITE;
                        string productName = detail.Product?.Name ?? "Producto sin nombre";
                        int quantity = detail.Quantity;
                        decimal originalPrice = (detail.Product != null) ? detail.Product.Price : detail.UnitPrice;
                        decimal discountPercentage = (detail.Product != null && detail.Product.IsOnOffer) ? detail.Product.DiscountPercentage : 0;
                        decimal effectivePrice = (discountPercentage > 0) ? originalPrice * (1 - discountPercentage / 100m) : originalPrice;
                        decimal lineTotal = effectivePrice * quantity;

                        productTable.AddCell(new PdfPCell(new Phrase(productName, regularFont))
                        {
                            BackgroundColor = rowBackground,
                            Padding = 5
                        });
                        productTable.AddCell(new PdfPCell(new Phrase(quantity.ToString(), regularFont))
                        {
                            BackgroundColor = rowBackground,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5
                        });
                        productTable.AddCell(new PdfPCell(new Phrase(discountPercentage > 0 ? originalPrice.ToString("C") : "-", regularFont))
                        {
                            BackgroundColor = rowBackground,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5
                        });
                        productTable.AddCell(new PdfPCell(new Phrase(discountPercentage > 0 ? discountPercentage.ToString("F2") : "-", regularFont))
                        {
                            BackgroundColor = rowBackground,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5
                        });
                        productTable.AddCell(new PdfPCell(new Phrase(effectivePrice.ToString("C"), regularFont))
                        {
                            BackgroundColor = rowBackground,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5
                        });
                        productTable.AddCell(new PdfPCell(new Phrase(lineTotal.ToString("C"), regularFont))
                        {
                            BackgroundColor = rowBackground,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 5
                        });
                    }
                }
                else
                {
                    PdfPCell emptyCell = new PdfPCell(new Phrase("No hay productos en esta orden", regularFont))
                    {
                        Colspan = 6,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    };
                    productTable.AddCell(emptyCell);
                }
                document.Add(productTable);

                // ================================================================
                // Totales y Desglose del Pago con Borde Superior Destacado
                // ================================================================
                PdfPTable totalTable = new PdfPTable(2) { WidthPercentage = 100, SpacingBefore = 10 };
                totalTable.SetWidths(new float[] { 3, 1 });
                totalTable.AddCell(new PdfPCell(new Phrase("Subtotal:", boldFont))
                {
                    Border = PdfPCell.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5
                });
                totalTable.AddCell(new PdfPCell(new Phrase(subTotal.ToString("C"), regularFont))
                {
                    Border = PdfPCell.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5
                });
                totalTable.AddCell(new PdfPCell(new Phrase("IVA (15%):", boldFont))
                {
                    Border = PdfPCell.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5
                });
                totalTable.AddCell(new PdfPCell(new Phrase(totalIva.ToString("C"), regularFont))
                {
                    Border = PdfPCell.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5
                });
                totalTable.AddCell(new PdfPCell(new Phrase("Total:", boldFont))
                {
                    Border = PdfPCell.TOP_BORDER,
                    BorderColorTop = primaryColor,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5
                });
                totalTable.AddCell(new PdfPCell(new Phrase((subTotal + totalIva).ToString("C"), boldFont))
                {
                    Border = PdfPCell.TOP_BORDER,
                    BorderColorTop = primaryColor,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5
                });
                document.Add(totalTable);

                // ================================================================
                // Footer Ultra Premium: Información de Contacto y Marca de Agua Sutil
                // ================================================================
                PdfPTable footerTable = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 20 };
                PdfPCell footerCell = new PdfPCell(new Phrase("Gracias por su compra - DonJulio Super | www.donjuliosuper.com", footerFont))
                {
                    Border = PdfPCell.TOP_BORDER,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    PaddingTop = 10,
                    BorderColorTop = new BaseColor(200, 200, 200)
                };
                footerTable.AddCell(footerCell);
                document.Add(footerTable);

                // Marca de Agua Sutil con Transparencia
                PdfContentByte canvas = writer.DirectContentUnder;
                BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.EMBEDDED);
                canvas.BeginText();
                canvas.SetColorFill(new BaseColor(220, 220, 220));
                canvas.SetFontAndSize(bf, 80);
                canvas.ShowTextAligned(Element.ALIGN_CENTER, "DONJULIO SUPER", 297.5f, 421, 45);
                canvas.EndText();

                document.Close();
                return workStream.ToArray();
            }
        }


    }
}
