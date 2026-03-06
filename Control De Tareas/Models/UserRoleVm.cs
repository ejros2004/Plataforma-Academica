using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class UserRoleVm
    {
        public Guid UserRoleId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un usuario")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        public Guid RoleId { get; set; }

        public DateTime CreateAt { get; set; }
        public bool IsSoftDeleted { get; set; }
    }
}