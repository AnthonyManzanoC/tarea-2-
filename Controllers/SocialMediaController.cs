using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Threading.Tasks;

namespace CoronelExpress.Controllers
{
    public class SocialMediaController : Controller
    {
        // Muestra la vista con los botones para las redes sociales
        public IActionResult Compose()
        {
            return View();
        }

        // Acción para abrir Telegram Web
        [HttpGet]
        public async Task<IActionResult> Telegram()
        {
            try
            {
                await OpenSiteWithSelenium("https://web.telegram.org/");
                return RedirectToAction("Close");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error al abrir Telegram: " + ex.Message });
            }
        }

        // Acción para abrir Instagram
        [HttpGet]
        public async Task<IActionResult> Instagram()
        {
            try
            {
                await OpenSiteWithSelenium("https://www.instagram.com/");
                return RedirectToAction("Close");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error al abrir Instagram: " + ex.Message });
            }
        }

        // Acción para abrir Facebook
        [HttpGet]
        public async Task<IActionResult> Facebook()
        {
            try
            {
                await OpenSiteWithSelenium("https://www.facebook.com/");
                return RedirectToAction("Close");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error al abrir Facebook: " + ex.Message });
            }
        }

        // Vista de cierre que muestra un mensaje y un botón para volver al Admin Dashboard
        public IActionResult Close()
        {
            return View();
        }

        // Método privado que utiliza Selenium para abrir la URL especificada
        private async Task OpenSiteWithSelenium(string url)
        {
            string driverPath = @"C:\Users\USER\source\repos\CoronelExpress\Drivers";
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");

            using (IWebDriver driver = new ChromeDriver(driverPath, options))
            {
                try
                {
                    driver.Navigate().GoToUrl(url);
                    // Se espera 10 segundos para que el usuario interactúe con la web
                    await Task.Delay(10000);
                }
                finally
                {
                    driver.Quit();
                }
            }
        }
    }
}
