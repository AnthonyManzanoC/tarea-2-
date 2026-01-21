using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoronelExpress.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // ID del usuario en AspNetUsers

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        public string ProfileImagePath { get; set; } // Ruta de la imagen
    }
}
