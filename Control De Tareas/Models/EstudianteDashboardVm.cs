namespace Control_De_Tareas.Models
{
    public class EstudianteDashboardVm
    {
        public string NombreEstudiante { get; set; } = "Estudiante";

        // Cursos
        public List<CursoInscritoVm> CursosInscritos { get; set; } = new();

        // Tareas pendientes
        public List<EntregaPendienteVm> TareasPendientes { get; set; } = new();

        // Próximas tareas (tarjeta superior)
        public List<TareaVm> ProximasEntregas { get; set; } = new();

        // Calificaciones
        public List<CalificacionVm> UltimasCalificaciones { get; set; } = new();

        public decimal? PromedioGeneral { get; set; }

        // Datos agrupados por curso (para el accordion)
        public List<TareasPorCursoVm> PendientesPorCurso { get; set; } = new();
    }
}
