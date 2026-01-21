using CoronelExpress.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CoronelExpress.Controllers
{ 
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;


        public HomeController(IProductService productService, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _productService = productService;
            _userManager = userManager;
            _signInManager = signInManager;
        }


        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        [Route("Nosotros")]
        public IActionResult Nosotros()
        {
            return View();
        }

        [Route("TrabajaConNosotros")]
        public IActionResult TrabajaConNosotros()
        {
            return View();
        }

        [Route("FacturasElectronicas")]
        public IActionResult FacturasElectronicas()
        {
            return View();
        }

        [Route("OfertasFalsas")]
        public IActionResult OfertasFalsas()
        {
            return View();
        }

        [Route("Proveedores")]
        public IActionResult Proveedores()
        {
            return View();
        }

        [Route("PreguntasFrecuentes")]
        public IActionResult PreguntasFrecuentes()
        {
            return View();
        }

        [Route("Terminos")]
        public IActionResult Terminos()
        {
            return View();
        }
        public ActionResult Condiciones()
        {
            return View("Condiciones"); // Asegúrate de que el nombre "Terms" coincida con el de tu vista.
        }


        [Route("Privacidad")]
        public IActionResult Privacidad()
        {
            return View();
        }

        [Route("Convenio")]
        public IActionResult Convenio()
        {
            return View();
        }

        [Route("DatosPersonales")]
        public IActionResult DatosPersonales()
        {
            return View();
        }

        [Route("CF")]
        public IActionResult CF()
        {
            return View();
        }

        [Route("EnlacesExternos")]
        public IActionResult EnlacesExternos()
        {
            return View();
        }
     [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                await _signInManager.SignOutAsync();
            }
            return RedirectToAction("Goodbye","Home");
        }

        public IActionResult Goodbye()
        {
            return View();
        }
    }
}


