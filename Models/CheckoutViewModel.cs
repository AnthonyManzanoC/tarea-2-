using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoronelExpress.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Seleccione un método de pago.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un método de pago válido.")]
        public int PaymentMethodId { get; set; }
        // NUEVO: Propiedad para capturar el RUC/CI del comprador
        [Required(ErrorMessage = "El RUC/CI es obligatorio.")]
        public string RUC { get; set; }


        public List<CartItem> CartItems { get; set; } = new List<CartItem>();

        public List<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

        public decimal TotalAmount { get; set; } // Ahora puedes asignar valores a TotalAmount

    }
}
