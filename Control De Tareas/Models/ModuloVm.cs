namespace Control_De_Tareas.Models
{
    public class ModuloVm

    {
        public Guid ModuleId { get; set; }
        public string Nombre { get; set; }

        public string Metodo { get; set; }
        public string Controlador { get; set; }
        public Guid ModuloAgrupadoId { get; set; }



    }
}
