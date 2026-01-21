using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Threading.Tasks;

namespace CoronelExpress.Controllers
{
    // Modelo para recibir la información de WhatsApp
    public class WhatsAppRequestModel
    {
        public string Telefono { get; set; }  // Opcional
        public string Mensaje { get; set; }     // Opcional
    }

    public class WhatsAppController : Controller
    {
        // Muestra la vista para ingresar los datos de WhatsApp
        public IActionResult Compose()
        {
            return View();
        }

        // Acción POST que procesa la información y utiliza Selenium para abrir WhatsApp
        [HttpPost]
        public async Task<IActionResult> Send(WhatsAppRequestModel request)
        {
            try
            {
                await EnviarWhatsAppConSelenium(request.Telefono, request.Mensaje);
                // Una vez finalizado el proceso, redirige a la vista de cierre
                return RedirectToAction("Close");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error al enviar WhatsApp: " + ex.Message });
            }
        }

        // Vista de cierre que indica que el proceso finalizó y ofrece volver al Dashboard
        public IActionResult Close()
        {
            return View();
        }

        // Método que utiliza Selenium para abrir WhatsApp Web y, si es posible, enviar el mensaje
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
                    // Ingresar a WhatsApp Web y esperar la autenticación
                    driver.Navigate().GoToUrl("https://web.whatsapp.com/");
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(1000));
                    wait.Until(drv =>
                    {
                        try
                        {
                            // Si aparece el canvas del QR, aún no se ha autenticado
                            return drv.FindElement(By.CssSelector("canvas[aria-label='Scan me!']")) == null;
                        }
                        catch
                        {
                            return true;
                        }
                    });

                    // Si se ingresa teléfono o mensaje, se arma la URL
                    if (!string.IsNullOrWhiteSpace(telefono) || !string.IsNullOrWhiteSpace(mensaje))
                    {
                        string url = "https://web.whatsapp.com/send?";
                        if (!string.IsNullOrWhiteSpace(telefono))
                        {
                            url += $"phone={telefono}";
                        }
                        if (!string.IsNullOrWhiteSpace(mensaje))
                        {
                            url += (!string.IsNullOrWhiteSpace(telefono) ? "&" : "") + $"text={Uri.EscapeDataString(mensaje)}";
                        }
                        driver.Navigate().GoToUrl(url);

                        // Si se proporcionó teléfono, esperamos a que se cargue el chat
                        if (!string.IsNullOrWhiteSpace(telefono))
                        {
                            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//div[@title='Escribe un mensaje aquí']")));
                            // Si además se ingresó mensaje, se intenta enviar automáticamente
                            if (!string.IsNullOrWhiteSpace(mensaje))
                            {
                                var sendButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[@data-testid='compose-btn-send']")));
                                sendButton.Click();
                                await Task.Delay(3000);
                            }
                            else
                            {
                                // Si solo se ingresó teléfono, se da tiempo para que el usuario interactúe
                                await Task.Delay(10000);
                            }
                        }
                        else
                        {
                            // Si se ingresó solo mensaje, se abre la URL y se espera
                            await Task.Delay(10000);
                        }
                    }
                    else
                    {
                        // Si ambos campos están vacíos, se abre WhatsApp Web general
                        driver.Navigate().GoToUrl("https://web.whatsapp.com/");
                        await Task.Delay(10000);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error en Selenium: " + ex.Message);
                }
                finally
                {
                    await Task.Delay(5000);
                    driver.Quit();
                }
            }
        }
    }
}
