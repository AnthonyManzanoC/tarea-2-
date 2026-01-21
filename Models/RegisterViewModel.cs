using System.ComponentModel.DataAnnotations;

namespace CoronelExpress.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Debe confirmar la contraseña")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }
        [Required(ErrorMessage = "La pregunta de seguridad es obligatoria")]
        public string SecurityQuestion { get; set; }

        [Required(ErrorMessage = "La respuesta de seguridad es obligatoria")]
        public string SecurityAnswer { get; set; }
    }
}
