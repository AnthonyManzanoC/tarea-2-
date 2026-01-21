

    using CoronelExpress.Helpers;
    using CoronelExpress.Models;
    using CoronelExpress.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    namespace CoronelExpress.Controllers
    {
         public class CartController : Controller
    {
        private const string CartSessionKey = "cart";
        private readonly IProductService _productService;

        public CartController(IProductService productService)
        {
            _productService = productService;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // GET: /Cart/Add/1
        public async Task<IActionResult> Add(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(); // No existe el producto
            }

            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(c => c.Product.Id == id);

            if (cartItem != null)
            {
                // Verifica stock antes de aumentar
                if (cartItem.Quantity < product.Stock)
                {
                    cartItem.Quantity++;
                }
                else
                {
                    TempData["Error"] = "No hay suficiente stock disponible.";
                }
            }
            else
            {
                if (product.Stock > 0)
                {
                    cart.Add(new CartItem { Product = product, Quantity = 1 });
                }
                else
                {
                    TempData["Error"] = "Producto agotado.";
                }
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // GET: /Cart/Remove/1
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(c => c.Product.Id == id);

            if (cartItem != null)
            {
                if (cartItem.Quantity > 1)
                {
                    cartItem.Quantity--;
                }
                else
                {
                    cart.Remove(cartItem);
                }
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(c => c.Product.Id == id);

            if (cartItem != null && quantity > 0)
            {
                if (quantity <= cartItem.Product.Stock)
                {
                    cartItem.Quantity = quantity;
                }
                else
                {
                    TempData["Error"] = "Stock insuficiente.";
                }
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey);
            return cart ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
        }
    }
}
