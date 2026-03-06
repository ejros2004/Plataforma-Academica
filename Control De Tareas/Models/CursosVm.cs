namespace Control_De_Tareas.Models
{
    public class CursosVm
    {
        public List<CursoDto> Cursos { get; set; } = new List<CursoDto>();
    }

    public class CursoDto
    {
        public Guid Id { get; set; }  // ID del CourseOffering
        public Guid CursoId { get; set; } // ID del Course base
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string InstructorNombre { get; set; } = string.Empty;
        public int CantidadEstudiantes { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
    }

    public class CourseBaseVm
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public bool HasOfferings { get; set; }
    }
}