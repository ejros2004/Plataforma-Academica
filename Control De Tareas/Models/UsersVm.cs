using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Models
{
    public class UserVm
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }

        [NotMapped] // Esta propiedad no se mapea a la base de datos
        [Compare("PasswordHash", ErrorMessage = "Las contraseñas no coinciden")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public RolVm Rol { get; set; }
        public List<ModuleGroupVm> menu { get; set; }
    }
}