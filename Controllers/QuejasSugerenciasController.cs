using CoronelExpress.Data;
using CoronelExpress.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace CoronelExpress.Controllers
{
    public class QuejasSugerenciasController : Controller
    {
        private readonly ApplicationDbContext _context;
        public QuejasSugerenciasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /QuejasSugerencias
        public IActionResult Index()
        {
            return View();
        }

        // POST: /QuejasSugerencias
        [HttpPost]
        public async Task<IActionResult> Index(QuejaSugerenciaMessage model)
        {
            if (ModelState.IsValid)
            {
                model.Fecha = DateTime.Now;
                _context.QuejaSugerenciaMessages.Add(model);
                await _context.SaveChangesAsync();

                try
                {
                    using (var client = new SmtpClient("smtp.gmail.com", 587))
                    {
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                        client.EnableSsl = true;

                        // Construir el template HTML corporativo para la notificación
                        var htmlBody = new StringBuilder();
                        htmlBody.AppendLine("<!DOCTYPE html>");
                        htmlBody.AppendLine("<html lang='es'>");
                        htmlBody.AppendLine("<head>");
                        htmlBody.AppendLine("    <meta charset='UTF-8'>");
                        htmlBody.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                        htmlBody.AppendLine("    <title>Confirmación de Recepción</title>");
                        htmlBody.AppendLine("    <style>");
                        htmlBody.AppendLine("        body { font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }");
                        htmlBody.AppendLine("        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border: 1px solid #e0e0e0; }");
                        htmlBody.AppendLine("        .header { background-color: #003366; padding: 20px; text-align: center; color: #ffffff; }");
                        htmlBody.AppendLine("        .content { padding: 20px; }");
                        htmlBody.AppendLine("        .content p { font-size: 16px; line-height: 1.6; color: #666666; }");
                        htmlBody.AppendLine("        .footer { background-color: #003366; padding: 10px; text-align: center; color: #ffffff; font-size: 14px; }");
                        htmlBody.AppendLine("    </style>");
                        htmlBody.AppendLine("</head>");
                        htmlBody.AppendLine("<body>");
                        htmlBody.AppendLine("    <div class='container'>");
                        htmlBody.AppendLine("        <div class='header'>");
                        htmlBody.AppendLine("            <h1>Sistema CoronelExpress</h1>");
                        htmlBody.AppendLine("        </div>");
                        htmlBody.AppendLine("        <div class='content'>");
                        htmlBody.AppendLine($"           <p>Hola {model.Nombre},</p>");
                        htmlBody.AppendLine("           <p>Hemos recibido tu queja/sugerencia. Agradecemos tu retroalimentación y nos pondremos en contacto contigo a la brevedad.</p>");
                        htmlBody.AppendLine("           <p>Muchas gracias por confiar en nosotros.</p>");
                        htmlBody.AppendLine("        </div>");
                        htmlBody.AppendLine("        <div class='footer'>");
                        htmlBody.AppendLine("            <p>Notificación enviada automáticamente por el sistema CoronelExpress</p>");
                        htmlBody.AppendLine("            <p>&copy; " + DateTime.Now.Year + " CoronelExpress. Todos los derechos reservados.</p>");
                        htmlBody.AppendLine("        </div>");
                        htmlBody.AppendLine("    </div>");
                        htmlBody.AppendLine("</body>");
                        htmlBody.AppendLine("</html>");

                        MailMessage mail = new MailMessage
                        {
                            From = new MailAddress("noreply@tuproject.com", "CoronelExpress"),
                            Subject = "Confirmación de Recepción de Queja/Sugerencia",
                            Body = htmlBody.ToString(),
                            IsBodyHtml = true
                        };
                        mail.To.Add(model.Email);

                        client.Send(mail);
                    }
                    TempData["Success"] = "Tu queja/sugerencia ha sido enviada exitosamente.";
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
