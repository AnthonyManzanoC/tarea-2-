using System.ComponentModel.DataAnnotations;

namespace CoronelExpress.Models
{
    public class TrabajaConNosotros
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        public string Mensaje { get; set; }

        // Esta propiedad almacenará la ruta del archivo subido
        public string HojaVidaPath { get; set; }
    }
}
