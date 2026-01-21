namespace CoronelExpress.Models
{
    public class ManageUserRolesViewModel
    {
        public string UserId { get; set; }  // ID del usuario
        public string UserName { get; set; }  // Nombre de usuario
        public string Email { get; set; }  // Correo electrónico
        public string PhoneNumber { get; set; }  // Número de teléfono
        public bool LockoutEnabled { get; set; }  // Indica si el usuario está bloqueado
        public List<string> UserRoles { get; set; }  // Roles actuales del usuario
        public List<string> AvailableRoles { get; set; }  // Todos los roles disponibles
    }
    public class RoleSelectionViewModel
    {
        public string RoleName { get; set; }
        public bool Selected { get; set; }
    }
}