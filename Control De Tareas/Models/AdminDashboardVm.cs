namespace Control_De_Tareas.Models
{
    public class AdminDashboardVm
    {
        // Tarjetas principales
        public int TotalUsuarios { get; set; }
        public int TotalCursos { get; set; }
        public int TotalTareas { get; set; }

        public int TotalProfesores { get; set; }
        public int TotalEstudiantes { get; set; }

        // Actividad reciente (estilo Figma)
        public List<ActividadVm> ActividadReciente { get; set; } = new();
    }

    public class ActividadVm
    {
        public string Mensaje { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }  // User / Course / Audit
    }
}
