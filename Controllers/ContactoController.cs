using CoronelExpress.Data;
using CoronelExpress.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace CoronelExpress.Controllers
{
    public class ContactoController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ContactoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Contacto
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Contacto
        [HttpPost]
        public async Task<IActionResult> Index(ContactoMessage model)
        {
            if (ModelState.IsValid)
            {
                model.Fecha = DateTime.Now;
                _context.ContactoMessages.Add(model);
                await _context.SaveChangesAsync();

                try
                {
                    using (var client = new SmtpClient("smtp.gmail.com", 587))
                    {
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                        client.EnableSsl = true;

                        // Construcción del template HTML corporativo para la notificación de contacto
                        var htmlBody = new StringBuilder();
                        htmlBody.AppendLine("<!DOCTYPE html>");
                        htmlBody.AppendLine("<html lang='es'>");
                        htmlBody.AppendLine("<head>");
                        htmlBody.AppendLine("    <meta charset='UTF-8'>");
                        htmlBody.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                        htmlBody.AppendLine("    <title>Confirmación de Contacto</title>");
                        htmlBody.AppendLine("    <style>");
                        htmlBody.AppendLine("        body { font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }");
                        htmlBody.AppendLine("        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border: 1px solid #e0e0e0; }");
                        htmlBody.AppendLine("        .header { background-color: #003366; padding: 20px; text-align: center; color: #ffffff; }");
                        htmlBody.AppendLine("        .header h1 { margin: 0; font-size: 26px; }");
                        htmlBody.AppendLine("        .content { padding: 20px; }");
                        htmlBody.AppendLine("        .content p { font-size: 16px; line-height: 1.6; color: #666666; }");
                        htmlBody.AppendLine("        .footer { background-color: #003366; padding: 10px; text-align: center; color: #ffffff; font-size: 14px; }");
                        htmlBody.AppendLine("    </style>");
                        htmlBody.AppendLine("</head>");
                        htmlBody.AppendLine("<body>");
                        htmlBody.AppendLine("    <div class='container'>");
                        htmlBody.AppendLine("        <div class='header'>");
                        htmlBody.AppendLine("            <h1>CoronelExpress</h1>");
                        htmlBody.AppendLine("        </div>");
                        htmlBody.AppendLine("        <div class='content'>");
                        htmlBody.AppendLine($"           <p>Hola {model.Nombre},</p>");
                        htmlBody.AppendLine("           <p>Gracias por contactarnos. Hemos recibido tu mensaje y nos comunicaremos contigo a la brevedad.</p>");
                        htmlBody.AppendLine("           <p>Te agradecemos por confiar en nosotros.</p>");
                        htmlBody.AppendLine("        </div>");
                        htmlBody.AppendLine("        <div class='footer'>");
                        htmlBody.AppendLine("            <p>Notificación enviada automáticamente por el sistema CoronelExpress</p>");
                        htmlBody.AppendLine("            <p>&copy; " + DateTime.Now.Year + " CoronelExpress. Todos los derechos reservados.</p>");
                        htmlBody.AppendLine("        </div>");
                        htmlBody.AppendLine("    </div>");
                        htmlBody.AppendLine("</body>");
                        htmlBody.AppendLine("</html>");

                        var mail = new MailMessage
                        {
                            From = new MailAddress("noreply@tuproject.com", "CoronelExpress"),
                            Subject = "Confirmación de Contacto",
                            Body = htmlBody.ToString(),
                            IsBodyHtml = true
                        };
                        mail.To.Add(model.Email);

                        client.Send(mail);
                    }
                    TempData["Success"] = "Tu mensaje ha sido enviado exitosamente.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Ocurrió un error al enviar el correo de confirmación.";
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }
    }
}
