namespace Control_De_Tareas.Data.Entitys
{
    public class Module
    {
        public Guid ModuleId { get; set; }
        public string Nombre { get; set; }

        public string Controller { get; set; }
        public string Metodo { get; set; }


        public DateTime CreateAt { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid CreatBy { get; set; }
        public Guid ModifieBy { get; set; }

        public bool IsSoftDeleted { get; set; }

        public Guid ModuloAgrupadoId { get; set; }

        public ModuleGroup ModuloAgrupado { get; set; } 

        public ICollection<RoleModules> RoleModules { get; set; }

        public Module()
        {
            RoleModules = new HashSet<RoleModules>();
        }
    }
}
