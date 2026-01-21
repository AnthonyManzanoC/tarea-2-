using System.ComponentModel.DataAnnotations;

namespace CoronelExpress.Models
{
    public class ContactoMessage
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Asunto { get; set; }

        [Required]
        public string Mensaje { get; set; }

        public DateTime Fecha { get; set; }
    }
}
