using System.ComponentModel.DataAnnotations;

namespace CoronelExpress.Models
{
    public class TermsAcceptanceViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress(ErrorMessage = "Correo inválido.")]
        public string Email { get; set; }

        public string Phone { get; set; }

        // Puedes agregar Address si lo requieres
        // public string Address { get; set; }

        [Required(ErrorMessage = "Debes aceptar los términos y condiciones.")]
        public bool AcceptTerms { get; set; }
    }
}