using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoronelExpress.Models
{
    public class InventoryMovement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string Type { get; set; } = "Entrada"; // "Entrada" o "Salida"

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Quantity { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string? Notes { get; set; }

        public Product? Product { get; set; }

    }
}
