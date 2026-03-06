namespace Control_De_Tareas.Data.Entitys
{
    public class ModuleGroup
    {
        public Guid GroupModuleId { get; set; }
        public string Description { get; set; }
       
        public DateTime CreateAt { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid CreatBy { get; set; }
        public Guid ModifieBy { get; set; }

        public bool IsSoftDeleted { get; set; }

     
        public ICollection<Module> Modules { get; set; }

        public ModuleGroup()
        {
            Modules = new HashSet<Module>();
        }




    }
}
