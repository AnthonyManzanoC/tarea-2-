using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CoronelExpress.Models;
using CoronelExpress.Data;

namespace CoronelExpress.Controllers
{
    public class NewsletterController : Controller
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPromotionRepository _promotionRepository;

        public NewsletterController(ISubscriptionRepository subscriptionRepository, IPromotionRepository promotionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _promotionRepository = promotionRepository;
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            // Validar correo
            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                TempData["SubscriptionError"] = "Por favor, ingresa un correo electrónico válido.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            // Guardar suscripción en la base de datos
            var subscription = new Subscription
            {
                Email = email,
                SubscribedOn = DateTime.UtcNow
            };
            await _subscriptionRepository.AddSubscriptionAsync(subscription);

            // Generar token único y URL interna para la experiencia promocional
            var promoDetails = GeneratePromotionExperience();

            // Crear y guardar un registro de promoción asociado a la suscripción,
            // asignando "Pendiente" como valor por defecto para Prize.
            var promotion = new Promotion
            {
                Token = promoDetails.token,
                GeneratedOn = DateTime.UtcNow,
                SubscriptionId = subscription.Id, // Se asume que el Id se asigna al guardar la suscripción
                Prize = "Pendiente"  // Valor por defecto para Prize
            };
            await _promotionRepository.AddPromotionAsync(promotion);

            try
            {
                // Enviar correo promocional con imágenes publicitarias y enlace funcional
                await SendPromotionalEmailAsync(email, promoDetails);
            }
            catch (Exception)
            {
                TempData["SubscriptionError"] = "La suscripción se completó, pero ocurrió un error al enviar el correo promocional.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            TempData["SubscriptionSuccess"] = "¡Te has suscrito exitosamente! Revisa tu correo para descubrir nuestras promociones exclusivas.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Genera un token único y una URL interna que apunta al controlador Promotion
        private (string token, string promotionUrl) GeneratePromotionExperience()
        {
            string token = Guid.NewGuid().ToString("N").Substring(0, 8);
            // URL relativa a la acción Promocion en el PromotionController
            string promotionUrl = Url.Action("Promocion", "Promotion", new { token = token }, Request.Scheme);
            return (token, promotionUrl);
        }

        // Envía el correo promocional utilizando SMTP de Gmail y enviando imágenes publicitarias
        private async Task SendPromotionalEmailAsync(string email, (string token, string promotionUrl) promoDetails)
        {
            var mail = new MailMessage
            {
                From = new MailAddress("noreply@donjuliosuper.com", "Don Julio Súper"),
                Subject = "¡Descubre las promociones revolucionarias de Don Julio Súper!",
                IsBodyHtml = true,
                Body = $@"
                <html>
                  <body style='font-family: Poppins, sans-serif; background-color: #f8f9fa; padding:20px;'>
                    <h2>¡Bienvenido a la revolución de las promociones!</h2>
                    <p>Nos emociona presentarte una experiencia publicitaria innovadora y exclusiva.</p>
                    <p>Haz clic en el enlace para descubrir ofertas y promociones especiales:</p>
                    <p>
                      <a href='{promoDetails.promotionUrl}' 
                         style='background-color:#007bff; color:#fff; padding:10px 20px; text-decoration:none; border-radius:5px;'>
                         Ver Promociones
                      </a>
                    </p>
                    <hr/>
                    <h3>Ofertas Destacadas:</h3>
                    <div style='display:flex; flex-wrap:wrap; gap:20px;'>
                      <div style='flex:1; min-width:200px; text-align:center;'>
                        <img src='https://example.com/promotions/ad1.jpg' alt='Publicidad 1' style='max-width:100%; height:auto; border-radius:5px;'/>
                        <p>Oferta Imperdible 1</p>
                      </div>
                      <div style='flex:1; min-width:200px; text-align:center;'>
                        <img src='https://example.com/promotions/ad2.jpg' alt='Publicidad 2' style='max-width:100%; height:auto; border-radius:5px;'/>
                        <p>Oferta Imperdible 2</p>
                      </div>
                    </div>
                    <hr/>
                    <p>¡No te pierdas esta experiencia revolucionaria y aprovecha las mejores ofertas!</p>
                  </body>
                </html>"
            };
            mail.To.Add(email);

            using (var client = new SmtpClient("smtp.gmail.com", 587))
            {
                client.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                client.EnableSsl = true;
                await client.SendMailAsync(mail);
            }
        }
    }
}
