using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    [Table("UserRoles")]
    public class UserRoles
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public DateTime CreatAt { get; set; }

        public bool IsSoftDeleted { get; set; }
        public Users User { get; set; } = null!;
        public Roles Role { get; set; } = null!;
    }
}