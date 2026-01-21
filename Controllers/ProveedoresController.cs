using CoronelExpress.Data;
using CoronelExpress.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Net;
using System.Text;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CoronelExpress.Controllers
{
    [Route("Admin")]

    public class ProveedoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Proveedores/Register
        [HttpGet("Proveedores/Register")]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Proveedores/Register
        [HttpPost("Proveedores/Register")]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                _context.Proveedores.Add(proveedor);
                _context.SaveChanges();
                return RedirectToAction("ThankYou");
            }
            return View(proveedor);
        }

        // Vista de agradecimiento (opcional)
        [HttpGet("Proveedores/ThankYou")]
        public ActionResult ThankYou()
        {
            return View();
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
        [HttpGet("Proveedores")]
        public IActionResult Proveedores()
        {
            var providers = _context.Proveedores.ToList();
            return View("~/Views/Admin/Proveedores.cshtml", providers);
        }



        // POST: Admin/EnviarEmail
        [HttpPost("EnviarEmail")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarEmail(EmailRequest request)
        {
            try
            {
                string subject = "Notificación de Proveedor";
                string body = request.emailPreset + "" + request.emailAdditional;

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("tucorreo@ejemplo.com"); // Reemplaza con tu correo real
                mail.To.Add(request.recipientEmail);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = false;

                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
                smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                smtpClient.EnableSsl = true;

                await smtpClient.SendMailAsync(mail);
                TempData["SuccessMessage"] = "Email enviado correctamente.";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error al enviar email: " + ex.Message;
            }
            return RedirectToAction("Proveedores");
        }

        // POST: Admin/GenerarMensaje
        [HttpPost("GenerarMensaje")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarMensaje()
        {
            try
            {
                string mensajeGenerado = await GenerarMensajeConquistaAsync();
                TempData["SuccessMessage"] = "Mensaje generado: " + mensajeGenerado;
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error al generar mensaje: " + ex.Message;
            }
            return RedirectToAction("Proveedores");
        }

        private async Task<string> GenerarMensajeConquistaAsync()
        {
            string apiKey = "fyijob7XzpVfTi2oH8wqs32QsjV9pBiz"; // Reemplaza con tu API key real
            string prompt = "GENERAR UN MENSAJE EXCELENTE PARA PODER ENVIAR POR CORREO Y CONQUISTAR A UNA CHICA COLOMBIANA que impacte psicológicamente...";

            var requestBody = new
            {
                model = "mistralai/Mistral-7B-Instruct-v0.1",
                messages = new[]
                {
                    new { role = "system", content = "lograr seducción con ese mensaje para ella." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            string jsonRequestBody = JsonConvert.SerializeObject(requestBody);

            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                var content = new System.Net.Http.StringContent(jsonRequestBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.deepinfra.com/v1/openai/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject result = JObject.Parse(jsonResponse);
                    string mensaje = result["choices"]?[0]?["message"]?["content"]?.ToString();
                    if (string.IsNullOrEmpty(mensaje))
                        throw new System.Exception("No se recibió una respuesta válida de la API.");
                    return mensaje.Trim();
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    throw new System.Exception("Error en la API: " + response.ReasonPhrase + " - " + errorMsg);
                }
            }
        }
        [HttpGet("GenerarMensajeParaWhatsApp")]
        public async Task<IActionResult> GenerarMensajeParaWhatsAppGet(int providerId)
        {
            return await GenerarMensajeParaWhatsApp(providerId);
        }


        [HttpPost("GenerarMensajeParaWhatsApp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarMensajeParaWhatsApp(int providerId)
        {
            try
            {
                string mensajeGenerado = await GenerarMensajeConquistaAsync();
                mensajeGenerado = System.Net.WebUtility.HtmlDecode(mensajeGenerado);
                TempData["WhatsappMensajeGenerado"] = mensajeGenerado;
                TempData["whatsappProviderId"] = providerId;

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al generar mensaje: " + ex.Message;
            }
            return RedirectToAction("Proveedores");
        }

        // POST: Admin/EnviarWhatsApp
        [HttpPost("EnviarWhatsApp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarWhatsApp(WhatsAppRequest request)
        {
            try
            {
                string messageToSend = request.generatedMessage;
                if (!string.IsNullOrWhiteSpace(request.whatsappAdditional))
                {
                    messageToSend += "" + request.whatsappAdditional;
                }
                await EnviarWhatsAppConSelenium(request.recipientPhone, messageToSend);
                TempData["SuccessMessage"] = "Mensaje de WhatsApp enviado correctamente.";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error al enviar mensaje de WhatsApp: " + ex.Message;
            }
            return RedirectToAction("Proveedores");
        }

        private async Task EnviarWhatsAppConSelenium(string telefono, string mensaje)
        {
            string driverPath = @"C:\Users\USER\source\repos\CoronelExpress\Drivers";
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-data-dir=C:\\WhatsAppSelenium");
            options.AddArgument("--start-maximized");

            using (IWebDriver driver = new ChromeDriver(driverPath, options))
            {
                try
                {
                    driver.Navigate().GoToUrl("https://web.whatsapp.com/");
                    WebDriverWait wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(150));
                    wait.Until(drv =>
                    {
                        try
                        {
                            return drv.FindElement(By.CssSelector("canvas[aria-label='Scan me!']")) == null;
                        }
                        catch
                        {
                            return true;
                        }
                    });
                    driver.Navigate().GoToUrl($"https://web.whatsapp.com/send?phone={telefono}&text={Uri.EscapeDataString(mensaje)}");
                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath("//div[@title='Escribe un mensaje aquí']")));
                    var sendButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//button[@data-testid='compose-btn-send']")));
                    sendButton.Click();
                    await Task.Delay(3000);
                }
                catch (System.Exception ex)
                {
                    throw new System.Exception("Error en Selenium: " + ex.Message);
                }
                finally
                {
                    await Task.Delay(5000);
                    driver.Quit();
                }
            }
        }
    }

    // Clases para recibir los datos de los formularios
    public class EmailRequest
    {
        public int providerId { get; set; }
        public string recipientEmail { get; set; }
        public string emailPreset { get; set; }
        public string emailAdditional { get; set; }
    }

    public class WhatsAppRequest
    {
        public int providerId { get; set; }
        public string recipientPhone { get; set; }
        public string generatedMessage { get; set; }
        public string whatsappAdditional { get; set; }
    }
}