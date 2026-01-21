using CoronelExpress.Data;
using CoronelExpress.Models;
using System;
using System.Threading.Tasks;

namespace CoronelExpress.Services
{
    public class InventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Actualiza el stock del producto y registra un movimiento (Entrada o Salida).
        /// No llama a SaveChangesAsync, se espera que el caller lo invoque para persistir la transacción.
        /// </summary>
        /// <param name="product">Producto a actualizar</param>
        /// <param name="quantity">Cantidad a sumar o restar</param>
        /// <param name="movementType">"Entrada" o "Salida"</param>
        /// <param name="notes">Notas adicionales</param>
        public Task UpdateStockAsync(Product product, int quantity, string movementType, string notes = null)
        {
            if (movementType == "Entrada")
            {
                product.Stock += quantity;
            }
            else if (movementType == "Salida")
            {
                if (quantity > product.Stock)
                    throw new InvalidOperationException("No hay suficiente stock para realizar la salida.");
                product.Stock -= quantity;
            }
            else
            {
                throw new ArgumentException("Tipo de movimiento inválido. Use 'Entrada' o 'Salida'.");
            }

            var movement = new InventoryMovement
            {
                ProductId = product.Id,
                Type = movementType,
                Quantity = quantity,
                Date = DateTime.Now,
                Notes = notes
            };

            _context.InventoryMovement.Add(movement);
            _context.Products.Update(product);

            return Task.CompletedTask;
        }
    }
}
