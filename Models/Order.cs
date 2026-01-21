namespace CoronelExpress.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public int PaymentMethodId { get; set; }

        // Propiedad nueva para el número de orden
        public string OrderNumber { get; set; }
        public string? InvoiceEmail { get; set; } // Permitir valores nulos


        // Propiedades de navegación
        public Customer Customer { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
        // Nueva propiedad para el estado del pedido
        public string Status { get; set; } = "Pendiente"; // Estado predeterminado
        public string? ProcessCode { get; set; }
        public string? DeliveryConfirmationCode { get; set; }

        // NUEVO: Propiedad para el número secuencial de la factura
        public string? Sequential { get; set; }

    }
}
