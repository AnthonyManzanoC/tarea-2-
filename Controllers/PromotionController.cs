using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CoronelExpress.Models;
using CoronelExpress.Data;
using CoronelExpress.Migrations;
using AspNetCoreGeneratedDocument;

namespace CoronelExpress.Controllers
{
    [Route("Admin")]
    [Route("Promotion")]
    public class PromotionController : Controller
    {
        private readonly IPromotionRepository _promotionRepository;

        public PromotionController(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        [HttpGet("Promocion")]
        public async Task<IActionResult> Promocion(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["MensajeError"] = "El token de promoción es inválido o ha expirado.";
                return RedirectToAction("Index", "Home");
            }

            var promotion = await _promotionRepository.GetPromotionByTokenAsync(token);
            if (promotion == null)
            {
                TempData["MensajeError"] = "No se encontró promoción con ese token.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Token = token;
            ViewBag.Promotion = promotion;
            return View();
        }

        [HttpPost("Finalizar")]
        public async Task<IActionResult> Finalizar([FromBody] FinalizarPromotionRequest request)
        {
            // Validar que se reciba el token y el premio
            if (request == null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Prize))
            {
                return BadRequest(new { error = "Token o premio inválido." });
            }

            // Buscar la promoción por token
            var promotion = await _promotionRepository.GetPromotionByTokenAsync(request.Token);
            if (promotion == null)
            {
                return BadRequest(new { error = "No se encontró promoción con ese token." });
            }

            // Verificar que la promoción tenga una suscripción asociada
            if (promotion.Subscription == null || string.IsNullOrWhiteSpace(promotion.Subscription.Email))
            {
                return BadRequest(new { error = "No se encontró el correo del suscriptor." });
            }

            try
            {
                // Crear el mensaje de correo con el token y premio
                var mail = new MailMessage
                {
                    From = new MailAddress("noreply@donjuliosuper.com", "Don Julio Súper"),
                    Subject = "Detalles de tu premio",
                    IsBodyHtml = true,
                    Body = $@"
                        <html>
                          <body style='font-family: Poppins, sans-serif; background-color: #f8f9fa; padding:20px;'>
                            <h2>¡Gracias por participar!</h2>
                            <p>Tu token: <strong>{promotion.Token}</strong></p>
                            <p>Has ganado: <strong>{request.Prize}</strong></p>
                            <p>Utiliza tu premio en nuestra tienda.</p>
                          </body>
                        </html>"
                };

                mail.To.Add(promotion.Subscription.Email);

                // Configurar el cliente SMTP (asegúrate de que las credenciales y permisos sean correctos)
                using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Timeout = 10000; // 10 segundos de timeout

                    await smtpClient.SendMailAsync(mail);
                }

                return Ok(new { success = true, message = "Correo enviado correctamente." });
            }
            catch (SmtpException smtpEx)
            {
                // Log (en consola u otro mecanismo de logging) y retornar error específico
                Console.WriteLine($"Error SMTP: {smtpEx.StatusCode} - {smtpEx.Message}");
                return StatusCode(500, new { error = $"Error SMTP: {smtpEx.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
                return StatusCode(500, new { error = $"Ocurrió un error al enviar el correo: {ex.Message}" });
            }
        }

        // GET: Promotion/SearchPromotion?token=...
        [HttpGet("SearchPromotion")]
        public async Task<IActionResult> SearchPromotion(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                // Si no se ingresa token, muestra la vista sin modelo (la vista mostrará solo el formulario).
                return View();
            }

            var promotion = await _promotionRepository.GetPromotionByTokenAsync(token);
            if (promotion == null)
            {
                TempData["Error"] = "No se encontró promoción con ese token.";
            }
            return View(promotion);
        }

        // POST: Promotion/ChangeStatus
        [HttpPost("ChangeStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
            if (promotion == null)
            {
                TempData["Error"] = "Promoción no encontrada.";
                return RedirectToAction("SearchPromotion");
            }
            if (promotion.Prize.ToLower() != "pendiente")
            {
                TempData["Error"] = "La promoción no se encuentra en estado pendiente.";
                return RedirectToAction("SearchPromotion", new { token = promotion.Token });
            }

            // Actualizamos el estado a "otorgado"
            promotion.Prize = "otorgado";

            try
            {
                await _promotionRepository.UpdatePromotionAsync(promotion);

                // Envío de notificación vía SMTP
                var mail = new MailMessage
                {
                    From = new MailAddress("noreply@donjuliosuper.com", "Don Julio Súper"),
                    Subject = "Tu premio ha sido otorgado",
                    IsBodyHtml = true,
                    Body = $@"
                        <html>
                          <body style='font-family: Poppins, sans-serif; background-color: #f8f9fa; padding:20px;'>
                            <h2>¡Felicidades!</h2>
                            <p>Tu promoción con token <strong>{promotion.Token}</strong> ha sido marcada como <strong>otorgado</strong>.</p>
                            <p>Disfruta de tu premio en nuestra tienda.</p>
                          </body>
                        </html>"
                };

                mail.To.Add(promotion.Subscription.Email);

                using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Timeout = 10000;
                    await smtpClient.SendMailAsync(mail);
                }

                TempData["Success"] = "La promoción se actualizó y se envió el correo de notificación.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "La promoción se actualizó, pero hubo un error al enviar el correo: " + ex.Message;
            }

            return RedirectToAction("SearchPromotion", new { token = promotion.Token });
        }
       
        // DTO para la solicitud de finalización de promoción (se mantiene para la funcionalidad existente)
        public class FinalizarPromotionRequest
        {
            public string Token { get; set; }
            public string Prize { get; set; }
        }
    }
}
