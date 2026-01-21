using CoronelExpress.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace CoronelExpress.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedoresApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Endpoint para enviar email vía SMTP
        [HttpPost("EnviarEmail")]
        public async Task<IActionResult> EnviarEmail([FromBody] EmailRequest request)
        {
            try
            {
                string subject = "Notificación de Proveedor";
                string body = request.emailPreset + Environment.NewLine + request.emailAdditional;

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("tucorreo@ejemplo.com"); // Reemplaza con tu correo real
                mail.To.Add(request.recipientEmail);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = false;

                // Configura tu servidor SMTP según corresponda
                SmtpClient smtpClient = new SmtpClient("smtp.ejemplo.com", 587);
                smtpClient.Credentials = new NetworkCredential("tucorreo@ejemplo.com", "tuContraseña");
                smtpClient.EnableSsl = true;

                await smtpClient.SendMailAsync(mail);
                return Ok(new { success = true, message = "Email enviado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error al enviar email: " + ex.Message });
            }
        }

        // Endpoint para generar mensaje utilizando la API de IA
        [HttpPost("GenerarMensaje")]
        public async Task<IActionResult> GenerarMensaje()
        {
            try
            {
                string mensajeGenerado = await GenerarMensajeConquistaAsync();
                return Ok(new { success = true, mensaje = mensajeGenerado });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = "Error al generar mensaje: " + ex.Message });
            }
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

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.deepinfra.com/v1/openai/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject result = JObject.Parse(jsonResponse);
                    string mensaje = result["choices"]?[0]?["message"]?["content"]?.ToString();
                    if (string.IsNullOrEmpty(mensaje))
                        throw new Exception("No se recibió una respuesta válida de la API.");
                    return mensaje.Trim();
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error en la API: " + response.ReasonPhrase + " - " + errorMsg);
                }
            }
        }

        // Endpoint para enviar mensaje vía WhatsApp mediante Selenium
        [HttpPost("EnviarWhatsApp")]
        public async Task<IActionResult> EnviarWhatsApp([FromBody] WhatsAppRequest request)
        {
            try
            {
                string messageToSend = request.generatedMessage;
                if (!string.IsNullOrWhiteSpace(request.whatsappAdditional))
                {
                    messageToSend += Environment.NewLine + request.whatsappAdditional;
                }
                await EnviarWhatsAppConSelenium(request.recipientPhone, messageToSend);
                return Ok(new { success = true, message = "Mensaje de WhatsApp enviado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error al enviar mensaje de WhatsApp: " + ex.Message });
            }
        }

        private async Task EnviarWhatsAppConSelenium(string telefono, string mensaje)
        {
            // Actualiza la ruta al chromedriver según corresponda
            string driverPath = @"C:\Users\USER\source\repos\CoronelExpress\Drivers";
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-data-dir=C:\\WhatsAppSelenium");
            options.AddArgument("--start-maximized");

            using (IWebDriver driver = new ChromeDriver(driverPath, options))
            {
                try
                {
                    // Abre WhatsApp Web
                    driver.Navigate().GoToUrl("https://web.whatsapp.com/");

                    // Espera a que se complete la autenticación (cuando desaparezca el canvas del QR)
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(150));
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

                    // Navega a la URL para enviar el mensaje
                    driver.Navigate().GoToUrl($"https://web.whatsapp.com/send?phone={telefono}&text={Uri.EscapeDataString(mensaje)}");

                    // Espera a que el área de mensaje sea visible y el botón de enviar sea clicable
                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath("//div[@title='Escribe un mensaje aquí']")));
                    var sendButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//button[@data-testid='compose-btn-send']")));

                    sendButton.Click();
                    await Task.Delay(3000); // Espera para confirmar el envío
                }
                catch (Exception ex)
                {
                    throw new Exception("Error en Selenium: " + ex.Message);
                }
                finally
                {
                    await Task.Delay(5000); // Espera antes de cerrar el navegador (opcional)
                    driver.Quit();
                }
            }
        }
    }

    // Clases para recibir los datos de los formularios vía AJAX
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
