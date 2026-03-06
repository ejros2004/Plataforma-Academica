namespace Control_De_Tareas.Data.Entitys
{
    public class RoleModules
    {
        public Guid ModuleRoleId { get; set; }
        public string Description { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid CreatBy { get; set; }
        public Guid ModifieBy { get; set; }

        public bool IsSoftDeleted { get; set; }

        public Guid ModuleId { get; set; }
        public Guid RoleId { get; set; }

        public Module Module { get; set; }
        public Roles Role { get; set; }



    }
}
