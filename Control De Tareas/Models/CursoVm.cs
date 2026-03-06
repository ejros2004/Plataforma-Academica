namespace Control_De_Tareas.Models
{
    public class CursoVm
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
    }
}