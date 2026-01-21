using CoronelExpress.Data;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace CoronelExpress.Controllers.Admin
{
    [Area("Admin")]
    public class ContactoController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ContactoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Contacto
        public IActionResult Index()
        {
            var mensajes = _context.ContactoMessages.OrderByDescending(m => m.Fecha).ToList();
            return View(mensajes);
        }

  
        // GET: Admin/Contacto/Details/5
        public IActionResult Details(int id)
        {
            var mensaje = _context.ContactoMessages.FirstOrDefault(m => m.Id == id);
            if (mensaje == null)
            {
                return NotFound();
            }
            return View(mensaje);
        }

        // GET: Admin/Contacto/Reply/5
        public IActionResult Reply(int id)
        {
            var mensaje = _context.ContactoMessages.FirstOrDefault(m => m.Id == id);
            if (mensaje == null)
            {
                return NotFound();
            }
            // Pre-cargamos el email del contacto
            var model = new ReplyViewModel
            {
                ContactId = mensaje.Id,
                Email = mensaje.Email
            };
            return View(model);
        }

      [HttpPost]
public async Task<IActionResult> Reply(ReplyViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    try
    {
        // Configuración del cliente SMTP usando bloques "using" para liberar recursos
        using (var smtpClient = new SmtpClient("smtp.gmail.com"))
        {
            smtpClient.Port = 587;
            smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
            smtpClient.EnableSsl = true;

            // Preparar el mensaje de correo con un diseño corporativo avanzado
            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress("no-reply@example.com", "Coronel Express");
                mailMessage.To.Add(new MailAddress(model.Email));
                mailMessage.Subject = "Respuesta a su Mensaje de Contacto";
                mailMessage.IsBodyHtml = true;

                // Construir el cuerpo del correo en HTML con diseño corporativo
                var body = new StringBuilder();
                body.AppendLine("<!DOCTYPE html>");
                body.AppendLine("<html lang='es'>");
                body.AppendLine("<head>");
                body.AppendLine("    <meta charset='UTF-8'>");
                body.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                body.AppendLine("    <title>Respuesta a su Mensaje de Contacto</title>");
                body.AppendLine("    <style>");
                body.AppendLine("        body { margin: 0; padding: 0; background-color: #f4f4f4; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }");
                body.AppendLine("        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border: 1px solid #e0e0e0; }");
                body.AppendLine("        .header { background-color: #0056b3; padding: 20px; text-align: center; }");
                body.AppendLine("        .header img { max-width: 150px; }");
                body.AppendLine("        .header h1 { margin: 10px 0 0; color: #ffffff; font-size: 26px; }");
                body.AppendLine("        .content { padding: 30px; }");
                body.AppendLine("        .content h2 { color: #0056b3; font-size: 22px; margin-bottom: 10px; }");
                body.AppendLine("        .content p { font-size: 16px; line-height: 1.6; color: #333333; }");
                body.AppendLine("        .blockquote { margin: 20px 0; padding: 15px; background-color: #f9f9f9; border-left: 4px solid #0056b3; font-style: italic; }");
                body.AppendLine("        .button { display: inline-block; padding: 12px 25px; margin: 20px 0; background-color: #0056b3; color: #ffffff; text-decoration: none; border-radius: 4px; font-size: 16px; }");
                body.AppendLine("        .footer { background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 12px; color: #777777; border-top: 1px solid #e0e0e0; }");
                body.AppendLine("    </style>");
                body.AppendLine("</head>");
                body.AppendLine("<body>");
                body.AppendLine("    <div class='container'>");
                body.AppendLine("        <div class='header'>");
                body.AppendLine("            <img src='https://www.coronel-express.com/logo.png' alt='Coronel Express Logo' />");
                body.AppendLine("            <h1>Coronel Express</h1>");
                body.AppendLine("        </div>");
                body.AppendLine("        <div class='content'>");
                body.AppendLine("            <h2>Respuesta a su Mensaje de Contacto</h2>");
                body.AppendLine("            <p>Estimado(a) Cliente,</p>");
                body.AppendLine("            <p>Agradecemos que se haya comunicado con nosotros. A continuación, le presentamos nuestra respuesta:</p>");
                body.AppendLine("            <div class='blockquote'>");
                body.AppendLine("                " + model.Message);
                body.AppendLine("            </div>");
                body.AppendLine("            <p>Si necesita más información o desea contactarnos nuevamente, estamos a su disposición.</p>");
                body.AppendLine("            <a href='https://www.coronel-express.com/contacto' class='button'>Contactar Soporte</a>");
                body.AppendLine("            <p>Atentamente,<br/>El equipo de Don Julio Super</p>");
                body.AppendLine("        </div>");
                body.AppendLine("        <div class='footer'>");
                body.AppendLine("            <p>&copy; " + DateTime.Now.Year + " Coronel Express. Todos los derechos reservados.</p>");
                body.AppendLine("        </div>");
                body.AppendLine("    </div>");
                body.AppendLine("</body>");
                body.AppendLine("</html>");

                mailMessage.Body = body.ToString();

                // Envío asíncrono del correo
                await smtpClient.SendMailAsync(mailMessage);
            }
        }

        TempData["Success"] = "El mensaje fue enviado correctamente.";
    }
    catch (SmtpException)
    {
        TempData["Error"] = "Error al enviar el mensaje. Por favor, inténtelo nuevamente más tarde.";
    }
    catch (Exception)
    {
        TempData["Error"] = "Se produjo un error inesperado al enviar el mensaje.";
    }

    return RedirectToAction("Index");
}


        // POST: Admin/Contacto/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var mensaje = _context.ContactoMessages.FirstOrDefault(m => m.Id == id);
            if (mensaje != null)
            {
                _context.ContactoMessages.Remove(mensaje);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // ViewModel para la respuesta vía SMTP
        public class ReplyViewModel
        {
            public int ContactId { get; set; }
            public string Email { get; set; }
            [Required(ErrorMessage = "El mensaje es obligatorio.")]
            public string Message { get; set; }
        }
    }
}