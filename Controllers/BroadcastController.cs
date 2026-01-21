using CoronelExpress.Data;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace CoronelExpress.Controllers
{
    public class PromotionRequest
    {
        public string emailPreset { get; set; }
        public string emailAdditional { get; set; }
    }

    public class BroadcastController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BroadcastController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Muestra la vista para componer el email de promoción
        public IActionResult Compose()
        {
            return View();
        }

        // Envía el email de promoción a todos los suscriptores, incluyendo la imagen si se sube
        [HttpPost]
        public async Task<IActionResult> Send(PromotionRequest request, IFormFile imageFile)
        {
            try
            {
                byte[] imageData = null;
                if (imageFile != null && imageFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(ms);
                        imageData = ms.ToArray();
                    }
                }

                // Si hay imagen, se define el bloque HTML para mostrarla
                string imageHtml = "";
                if (imageData != null)
                {
                    imageHtml = "<p style='text-align: center;'><img src='cid:promoImage' style='max-width:100%; display:block; margin:20px auto;'/></p>";
                }

                string htmlTemplate = @"
<html>
  <head>
    <meta charset='utf-8'>
    <title>Promoción Exclusiva</title>
  </head>
  <body style='margin:0; padding:0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table align='center' border='0' cellpadding='0' cellspacing='0' width='600' style='border-collapse: collapse;'>
      <tr>
        <td align='center' bgcolor='#003366' style='padding: 40px 0; color: #ffffff; font-size: 28px; font-weight: bold;'>
          Sistema CoronelExpress
        </td>
      </tr>
      <tr>
        <td bgcolor='#ffffff' style='padding: 40px;'>
          <h1 style='font-size: 24px; color: #333333; margin-bottom: 20px;'>Notificación de Promoción</h1>
          <p style='font-size: 16px; line-height: 24px; color: #666666; margin-bottom: 20px;'>
            " + request.emailPreset + @"
          </p>
          <p style='font-size: 16px; line-height: 24px; color: #666666; margin-bottom: 30px;'>
            " + request.emailAdditional + @"
          </p>
          " + imageHtml + @"
          <p style='text-align: center;'>
            <a href='http://www.tusitio.com' style='background-color: #003366; color: #ffffff; padding: 10px 20px; text-decoration: none; font-size: 16px; border-radius: 4px;'>
              Ver Promoción
            </a>
          </p>
        </td>
      </tr>
      <tr>
        <td bgcolor='#003366' style='padding: 20px; text-align: center; color: #ffffff; font-size: 14px;'>
          © 2025 CoronelExpress. Todos los derechos reservados.<br/>
          Notificación enviada automáticamente por el sistema CoronelExpress.
        </td>
      </tr>
    </table>
  </body>
</html>";


                // Recuperar todos los emails suscritos
                var subscriptions = _context.Subscriptions.ToList();

                foreach (var subscription in subscriptions)
                {
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("tucorreo@ejemplo.com"); // Reemplaza con tu correo real
                    mail.To.Add(subscription.Email);
                    mail.Subject = "Notificación de Promoción a Clientes";
                    mail.IsBodyHtml = true;

                    // Crear el AlternateView para enviar el HTML con la imagen embebida
                    AlternateView avHtml = AlternateView.CreateAlternateViewFromString(htmlTemplate, null, "text/html");

                    // Agregar la imagen como recurso vinculado si se subió
                    if (imageData != null)
                    {
                        // Crear MemoryStream sin using para que no se cierre antes del envío
                        var msInline = new MemoryStream(imageData);
                        LinkedResource inline = new LinkedResource(msInline, imageFile.ContentType)
                        {
                            ContentId = "promoImage",
                            TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                        };
                        avHtml.LinkedResources.Add(inline);
                    }

                    mail.AlternateViews.Add(avHtml);

                    // Configuración del servidor SMTP (verifica que los datos sean correctos)
                    SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587)
                    {
                        Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw"),
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };

                    await smtpClient.SendMailAsync(mail);
                }

                return Ok(new { success = true, message = "Email enviado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error al enviar email: " + ex.Message });
            }
        }
    }
}