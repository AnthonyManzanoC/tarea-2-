using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CoronelExpress.Data;
using CoronelExpress.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoronelExpress.Controllers
{
    [Route("api/terms")]
    [ApiController]
    public class TermsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TermsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("accept")]
        public async Task<IActionResult> Accept([FromBody] TermsAcceptanceViewModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "El modelo no puede ser nulo." });
            }

            if (!ModelState.IsValid || !model.AcceptTerms)
            {
                return BadRequest(new { message = "Debes llenar todos los campos requeridos y aceptar los términos y condiciones." });
            }

            try
            {
                // Buscar cliente por email
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        Phone = model.Phone
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Verificar si el cliente ya ha aceptado los términos
                    bool alreadyAccepted = await _context.TermsAcceptances.AnyAsync(t => t.CustomerId == customer.Id);
                    if (alreadyAccepted)
                    {
                        return Ok(new { message = "Los términos y condiciones ya han sido aceptados previamente." });
                    }
                }

                // Crear registro de aceptación
                var acceptance = new TermsAcceptance
                {
                    CustomerId = customer.Id,
                    AcceptedAt = DateTime.UtcNow,
                    UserIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocido"
                };
                _context.TermsAcceptances.Add(acceptance);
                await _context.SaveChangesAsync();

                bool emailSent = await SendConfirmationEmail(customer.Email, customer.FullName);
                string emailStatus = emailSent
                    ? "Correo enviado correctamente."
                    : "No se pudo enviar el correo, pero la aceptación se guardó.";

                return Ok(new { message = "Términos aceptados correctamente. " + emailStatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al procesar la solicitud.", error = ex.ToString() });
            }
        }

        // Método para enviar el correo de confirmación vía SMTP con diseño institucional en HTML
        private async Task<bool> SendConfirmationEmail(string recipientEmail, string fullName)
        {
            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw"),
                    EnableSsl = true,
                })
                {
                    string body = $@"
            <!DOCTYPE html>
            <html lang='es'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Confirmación de Registro</title>
            </head>
            <body style='margin:0; padding:0; background-color:#f2f2f2;'>
                <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f2f2f2; padding:20px 0;'>
                    <tr>
                        <td align='center'>
                            <table width='600' cellpadding='0' cellspacing='0' border='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.1);'>
                                <!-- Encabezado -->
                                <tr>
                                    <td style='background-color:#004aad; padding:20px; text-align:center; color:#ffffff; font-size:24px; font-family:Arial, sans-serif;'>
                                        Confirmación de Aceptación de Términos
                                    </td>
                                </tr>
                                <!-- Contenido -->
                                <tr>
                                    <td style='padding:30px; font-family:Arial, sans-serif; color:#333333; font-size:16px; line-height:1.5;'>
                                        <p>Estimado/a <strong>{fullName}</strong>,</p>
                                        <p>Es un placer darte la bienvenida a <strong>Don Julio Súper</strong>. Nos complace informarte que has aceptado correctamente nuestros términos y condiciones.</p>
                                        <p>A partir de este momento, tendrás acceso a todos los servicios y beneficios exclusivos que ofrecemos. Te invitamos a explorar y disfrutar de una experiencia única.</p>
                                        <p style='text-align:center; margin:40px 0;'>
                                            <a href='https://donjuliosuper.com' style='background-color:#004aad; color:#ffffff; padding:15px 25px; text-decoration:none; border-radius:5px; font-weight:bold;'>Acceder a la Plataforma</a>
                                        </p>
                                        <p>Si tienes alguna duda o necesitas asistencia, no dudes en contactarnos. Estamos aquí para ayudarte.</p>
                                        <p>¡Gracias por confiar en nosotros!</p>
                                        <p>Atentamente,<br>
                                        <strong>Don Julio Súper | Comercial Don Julio</strong></p>
                                    </td>
                                </tr>
                                <!-- Pie de página -->
                                <tr>
                                    <td style='background-color:#f9f9f9; padding:20px; text-align:center; font-family:Arial, sans-serif; font-size:12px; color:#777777; border-top:1px solid #dddddd;'>
                                        © 2025 Don Julio Súper. Todos los derechos reservados.<br>
                                        Para soporte, escribe a <a href='mailto:soporte@donjuliosuper.com' style='color:#004aad; text-decoration:none;'>soporte@donjuliosuper.com</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("manzanocoroneljulioanthony@gmail.com", "Don Julio Súper"),
                        Subject = "Confirmación de Aceptación de Términos y Beneficio Exclusivo",
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(recipientEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en SendConfirmationEmail: " + ex.Message);
                return false;
            }
        }
    }
}
