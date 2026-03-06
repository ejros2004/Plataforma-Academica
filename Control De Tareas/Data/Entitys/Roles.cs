using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    [Table("Roles")]
    public class Roles
    {

        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public DateTime CreateAt { get; set; }
        public Guid modifiedBy { get; set; }
        public Boolean IsSoftDeleted { get; set; }



        public ICollection<Users> Users { get; set; }
        public ICollection<UserRoles> UserRoles { get; set; }
        public ICollection<RoleModules> RoleModules { get; set; }

        public Roles()
        {
            Users = new HashSet<Users>();
            UserRoles = new HashSet<UserRoles>();
            RoleModules = new HashSet<RoleModules>();
        }


    }
}