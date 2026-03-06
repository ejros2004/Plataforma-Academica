namespace Control_De_Tareas.Models
{
    public class ModuleGroupVm
    {
        public Guid GroupModuleId { get; set; }
        public string Descripcion { get; set; }


        public List<ModuloVm> Modulos { get; set; } 


    }
}
