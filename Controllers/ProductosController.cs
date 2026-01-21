using CoronelExpress.Data;
using CoronelExpress.Models;
using CoronelExpress.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CoronelExpress.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly InventoryService _inventoryService;

        public ProductosController(ApplicationDbContext context, InventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        // GET: Productos
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();

            return View(products);
        }

        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                // Capturamos el stock ingresado
                int initialStock = product.Stock;
                // Se asigna 0 para evitar duplicar el stock y permitir generar el Id correctamente
                product.Stock = 0;

                _context.Add(product);
                await _context.SaveChangesAsync();

                // Si el stock inicial es mayor a 0, se registra el movimiento de "Entrada"
                if (initialStock > 0)
                {
                    await _inventoryService.UpdateStockAsync(product, initialStock, "Entrada", "Stock inicial del producto");
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Se obtiene el stock original para comparar
                    var originalProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
                    if (originalProduct == null)
                        return NotFound();

                    int stockDifference = product.Stock - originalProduct.Stock;

                    // Si el stock es mayor a 0, se reinicia la notificación
                    if (product.Stock > 0)
                    {
                        product.NotifiedOutOfStock = false;
                    }

                    // Actualizamos el producto
                    _context.Update(product);

                    // Si hay diferencia en el stock, se utiliza el servicio para registrar el movimiento
                    if (stockDifference != 0)
                    {
                        // Se restablece el stock original para que el servicio sume o reste correctamente
                        product.Stock = originalProduct.Stock;
                        string movementType = stockDifference > 0 ? "Entrada" : "Salida";
                        await _inventoryService.UpdateStockAsync(product, Math.Abs(stockDifference), movementType, "Actualización de stock en edición");
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            // Eliminar registros en OrderDetails que referencian este producto
            var orderDetails = _context.OrderDetails.Where(od => od.ProductId == id);
            _context.OrderDetails.RemoveRange(orderDetails);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Productos/Search
        [HttpGet]
        public IActionResult Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return View("Search", null);
            }

            var product = _context.Products
                .Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
                .FirstOrDefault();

            return View("Search", product);
        }

        // GET: Productos/Disponibles (Solo productos con stock disponible)
        public async Task<IActionResult> Disponibles()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Stock > 0)
                .ToListAsync();

            return View(products);
        }

        // GET: Productos/EnOferta (Filtrar solo productos en oferta)
        public async Task<IActionResult> EnOferta()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsOnOffer)
                .ToListAsync();

            return View(products);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        
    }
}