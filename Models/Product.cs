namespace CoronelExpress.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsBasicBasket { get; set; }
        public bool NotifiedOutOfStock { get; set; } = false;

        // NUEVAS PROPIEDADES PARA OFERTAR
        public bool IsOnOffer { get; set; }  // Indica si el producto está en oferta
        public decimal DiscountPercentage { get; set; }  // Porcentaje de descuento, ej: 10 para 10%

        // Propiedad de navegación
        public Category? Category { get; set; }
    }
}

