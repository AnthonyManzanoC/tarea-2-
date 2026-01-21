using CoronelExpress.Data;
using CoronelExpress.Models;
using CoronelExpress.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RegisterPaymentAsync(Order order)
    {
        // Crear la transacción de cobro
        var payment = new PaymentTransaction
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,           // Asegúrate de que Order tenga este campo
            TransactionDate = DateTime.Now,
            TransactionType = "Ingreso",
            Status = "Cobrado",                   // O "Pendiente" si se requiere validación adicional
            PaymentMethod = order.PaymentMethod.Method     // "ContraEntrega", "Efectivo", "Cheque", "Transferencia"
        };

        // Si necesitas lógica específica según el método de pago, puedes usar un switch:
        switch (order.PaymentMethod.Method.ToLower())
        {
            case "contraentrega":
                // Lógica particular para pago contra entrega (si es necesario)
                break;
            case "efectivo":
            case "cheque":
            case "transferencia":
                // Puedes agregar validaciones o pasos adicionales para estos métodos
                break;
            default:
                // Por defecto se registra de manera estándar
                break;
        }

        _context.PaymentTransactions.Add(payment);
        await _context.SaveChangesAsync();
    }
}
