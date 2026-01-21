using System.ComponentModel.DataAnnotations.Schema;

namespace CoronelExpress.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }
        public int OrderId { get; set; }          // Relación con la orden
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        // Monto a cobrar (por ejemplo, order.TotalAmount)
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }  // "Ingreso"
        public string Status { get; set; }           // "Cobrado" o "Pendiente", según la lógica de negocio
        public string PaymentMethod { get; set; }    // "ContraEntrega", "Efectivo", "Cheque", "Transferencia"
    }
}
