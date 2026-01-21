using CoronelExpress.Data;
using CoronelExpress.Models;
using CoronelExpress.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CoronelExpress.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly InventoryService _inventoryService;
        public AdminController(ApplicationDbContext context,
                               UserManager<IdentityUser> userManager,
                               RoleManager<IdentityRole> roleManager, InventoryService inventoryService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _inventoryService = new InventoryService(context);
        }
        // GET: /Admin/SearchOrders
        public async Task<IActionResult> SearchOrders(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return View(new List<Order>()); // Devuelve una lista vacía si no hay búsqueda
            }

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderNumber.Contains(search) || o.Customer.FullName.Contains(search))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewData["Search"] = search;
            return View(orders);
        }

        // GET: /Admin/Dashboard
        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard(string search)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                ordersQuery = ordersQuery.Where(o => o.OrderNumber.Contains(search)
                    || o.Customer.FullName.Contains(search));
            }

            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();

            var totalProductos = await _context.Products.CountAsync();
            var ventasTotales = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var ordenesPendientes = await _context.Orders.CountAsync(o => o.Status == "Pendiente");

            ViewData["TotalProductos"] = totalProductos;
            ViewData["VentasTotales"] = ventasTotales.ToString("C");
            ViewData["TotalCategorias"] = await _context.Categories.CountAsync(); // Nuevo

            ViewData["Search"] = search;

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "ID de usuario inválido." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Usuario no encontrado." });

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Usuario eliminado correctamente." });
            }

            return Json(new { success = false, message = "Error al eliminar el usuario." });
        }

        // GET: /Admin/ManageUserRoles
        [HttpGet]
        public async Task<IActionResult> ManageUserRoles()
        {
            var users = await _userManager.Users.ToListAsync();
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var userRolesViewModel = new List<ManageUserRolesViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                userRolesViewModel.Add(new ManageUserRolesViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    LockoutEnabled = user.LockoutEnabled,
                    UserRoles = userRoles.ToList(),
                    AvailableRoles = roles
                });
            }

            return View(userRolesViewModel);
        }

        // POST: /Admin/UpdateUserRoles
        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return Json(new { success = false, message = "El ID del usuario es requerido." });
            }

            request.Roles ??= new List<string>();

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado." });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = request.Roles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(request.Roles).ToList();

            foreach (var role in rolesToAdd)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var newRole = new IdentityRole(role);
                    var roleResult = await _roleManager.CreateAsync(newRole);
                    if (!roleResult.Succeeded)
                    {
                        return Json(new { success = false, message = "Error al crear el rol.", errors = roleResult.Errors });
                    }
                }
            }

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    return Json(new { success = false, message = "Error al remover roles.", errors = removeResult.Errors });
                }
            }

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    return Json(new { success = false, message = "Error al agregar roles.", errors = addResult.Errors });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Roles actualizados correctamente." });
        }

        public class UpdateUserRolesRequest
        {
            public string UserId { get; set; }
            public List<string> Roles { get; set; }
        }

        // GET: /Admin/Index
        public async Task<IActionResult> Index(string search)
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o => o.OrderNumber.Contains(search) || o.Customer.FullName.Contains(search));
            }

            var orderList = await orders.ToListAsync();
            ViewData["Search"] = search;
            return View(orderList);
        }
        // GET: /Admin/OrdersByStatus
        public IActionResult OrdersByStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return RedirectToAction("Dashboard");
            }

            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.Status == status)
                .ToList();

            // Puedes pasar el estado a la vista para mostrarlo en el título
            ViewData["Status"] = status;
            return View(orders); // Asegúrate de tener la vista OrdersByStatus.cshtml
        }
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Solo se permite cancelar si la orden está pendiente
            if (order.Status != "Pendiente")
            {
                // Puedes retornar un mensaje de error o redireccionar informando que la orden ya no se puede cancelar
                return BadRequest("Solo se pueden cancelar órdenes en estado pendiente.");
            }

            // Revertir el stock para cada producto de la orden
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product != null)
                {
                    await _inventoryService.UpdateStockAsync(
                        detail.Product,
                        detail.Quantity,
                        "Entrada",
                        "Reversión cancelación de pedido"
                    );
                }
            }

            // Marcar la orden como cancelada
            order.Status = "Cancelled";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            // Redirecciona a la vista de listado o donde consideres apropiado
            return RedirectToAction("Dashboard");
        }
        // Acción para Promociones
        public IActionResult ManagePromotions(string token = null)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Buscar la promoción por el token ingresado
                var promotion = _context.Promotions
                    .Include(p => p.Subscription)
                    .FirstOrDefault(p => p.Token == token);

                if (promotion == null)
                {
                    TempData["Error"] = "No se encontró promoción con ese token.";
                    return View("Promotions", null); // Vista espera un solo objeto
                }

                return View("Promotions", promotion);
            }

            return View("Promotions", null); // No se ha buscado aún
        }


        // Acción para Proveedores
        public IActionResult ManageProveedores()
        {
            var proveedores = _context.Proveedores.ToList();
            return View("Proveedores", proveedores);
        }
        public IActionResult ManageTermsAcceptance()
        {
            var termsData = _context.TermsAcceptances
                .Include(t => t.Customer) // Incluir la relación con Customer
                .ToList();

            var viewModelList = termsData.Select(t => new TermsAcceptanceViewModel
            {
                FullName = t.Customer.FullName, // Acceder a FullName a través de Customer
                Email = t.Customer.Email, // Acceder a Email a través de Customer
                Phone = t.Customer.Phone, // Acceder a Phone a través de Customer
                AcceptTerms = true // Asignar un valor predeterminado ya que TermsAcceptance no tiene AcceptTerms
            }).ToList();

            return View(viewModelList);
        }
        [HttpPost]
        public IActionResult DeleteTermsAcceptance(string email)
        {
            // Buscar el registro que tenga el Email recibido (incluyendo la relación con Customer si es necesario)
            var record = _context.TermsAcceptances
                .Include(t => t.Customer)
                .FirstOrDefault(t => t.Customer.Email == email);

            if (record != null)
            {
                _context.TermsAcceptances.Remove(record);
                _context.SaveChanges();
            }
            return RedirectToAction("ManageTermsAcceptance");
        }

        // GET: Editar TrabajaConNosotros
        public IActionResult EditTrabajaConNosotros(int id)
        {
            var item = _context.TrabajaConNosotros.Find(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Editar TrabajaConNosotros
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTrabajaConNosotros(TrabajaConNosotros model)
        {
            if (ModelState.IsValid)
            {
                _context.TrabajaConNosotros.Update(model);
                _context.SaveChanges();
                return RedirectToAction("ManageTrabajaConNosotros");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTrabajaConNosotros(int id)
        {
            var item = _context.TrabajaConNosotros.Find(id);
            if (item != null)
            {
                _context.TrabajaConNosotros.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToAction("ManageTrabajaConNosotros");
        }



        // Acción para Trabaja con Nosotros
        public IActionResult ManageTrabajaConNosotros()
        {
            var trabaja = _context.TrabajaConNosotros.ToList();
            return View("TrabajaConNosotros", trabaja);
        }
    }

}
