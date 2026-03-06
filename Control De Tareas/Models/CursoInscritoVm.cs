namespace Control_De_Tareas.Models
{
    public class CursoInscritoVm
    {
        public Guid CourseOfferingId { get; set; } // CAMBIADO de int a Guid
        public string Curso { get; set; }
        public string Seccion { get; set; }
        public string Profesor { get; set; }
    }
}
