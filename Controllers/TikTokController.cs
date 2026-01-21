using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Threading.Tasks;

namespace CoronelExpress.Controllers
{
    public class TikTokController : Controller
    {
        public IActionResult Open()
        {
            return View();
        }

        // Acción para abrir TikTok Web
        [HttpGet]
        public async Task<IActionResult> OpenTikTok()
        {
            try
            {
                await OpenTikTokWithSelenium();
                return RedirectToAction("Close");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error al abrir TikTok: " + ex.Message });
            }
        }

        // Vista de cierre con mensaje y botón de salida
        public IActionResult Close()
        {
            return View();
        }

        // Método privado para abrir TikTok Web con Selenium
        private async Task OpenTikTokWithSelenium()
        {
            string driverPath = @"C:\Users\USER\source\repos\CoronelExpress\Drivers";
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");

            using (IWebDriver driver = new ChromeDriver(driverPath, options))
            {
                try
                {
                    driver.Navigate().GoToUrl("https://www.tiktok.com/");
                    await Task.Delay(10000); // Espera para que el usuario interactúe
                }
                finally
                {
                    driver.Quit();
                }
            }
        }
    }
}
