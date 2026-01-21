using CoronelExpress.Data;
using CoronelExpress.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoronelExpress.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context; // Añadir el DbContext

        public ProfileController(UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context; // Inicializar el DbContext
        }

        // Acción para mostrar el perfil del usuario
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new ProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
            };

            // Buscar la imagen del usuario en la base de datos
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserId == user.Id);
            if (userProfile != null && !string.IsNullOrEmpty(userProfile.ProfileImagePath))
            {
                ViewBag.ProfileImagePath = userProfile.ProfileImagePath;
            }
            else
            {
                ViewBag.ProfileImagePath = "/images/default-profile.png";
            }

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> UploadImageAjax(IFormFile imageFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Usuario no autenticado." });
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return Json(new { success = false, message = "No se seleccionó ningún archivo." });
            }

            // Carpeta de destino
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Genera un nombre basado en el ID del usuario
            string fileName = $"{user.Id}.png";
            string filePath = Path.Combine(uploadsFolder, fileName);

            // Guarda el archivo en el servidor
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // Guarda la ruta en la base de datos
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserId == user.Id);
            if (userProfile == null)
            {
                userProfile = new UserProfile { UserId = user.Id, ProfileImagePath = $"/uploads/{fileName}" };
                _context.UserProfiles.Add(userProfile);
            }
            else
            {
                userProfile.ProfileImagePath = $"/uploads/{fileName}";
                _context.UserProfiles.Update(userProfile);
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true, imageUrl = $"/uploads/{fileName}" });
        }


        [HttpGet]
        public async Task<IActionResult> SearchOrders(string ruc)
        {
            if (string.IsNullOrEmpty(ruc))
            {
                return Json(new { success = false, message = "El número de RUC es requerido." });
            }

            // Se asume que tienes un DbContext (_context) inyectado y que el modelo Customer tiene la propiedad RUC.
            var orders = await _context.Orders
                           .Where(o => o.Customer.RUC == ruc)
                           .Select(o => new {
                               o.OrderNumber,
                               OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                               o.TotalAmount,
                               o.Status
                           })
                           .ToListAsync();

            if (orders == null || !orders.Any())
            {
                return Json(new { success = false, message = "No se encontraron órdenes para el RUC proporcionado." });
            }

            return Json(new { success = true, orders = orders });
        }


    }
}