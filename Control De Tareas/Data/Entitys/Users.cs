

namespace Control_De_Tareas.Data.Entitys
{
    
    public class Users
    {

        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Instructor { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreateAt { get; set; }
        public Guid CreatBy { get; set; }
        public Guid ModifieBy { get; set; }
        public bool IsSoftDeleted { get; set; }
         

        //llave foranea 
        public Guid RolId { get; set; }
        public Roles Rol { get; set; }

        // Navigation properties
        public ICollection<UserRoles> UserRoles { get; set; }

        public Users()
        {
            UserRoles = new HashSet<UserRoles>();
        }

      
    }
}