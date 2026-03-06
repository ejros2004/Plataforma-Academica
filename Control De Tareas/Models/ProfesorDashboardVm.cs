namespace Control_De_Tareas.Models
{
    public class ProfesorDashboardVm
    {
        public string Profesor { get; set; }

        // Cursos asignados
        public List<CursoVm> Cursos { get; set; } = new();

        // Tareas creadas por el profesor
        public List<TareaVm> TareasPorCalificar { get; set; } = new();

        // Nuevas tarjetas (Figma)
        public int TareasActivas { get; set; }
        public int PendientesCalificar { get; set; }

        // Entregas próximas
        public List<EntregaProximaProfesorVm> ProximasEntregas { get; set; } = new();
    }

    public class EntregaProximaProfesorVm
    {
        public string CourseTitle { get; set; }
        public string TaskTitle { get; set; }
        public DateTime DueDate { get; set; }
        public int StudentsCount { get; set; }
    }
}
