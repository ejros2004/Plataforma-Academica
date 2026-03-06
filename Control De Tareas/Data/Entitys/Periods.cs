using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    /// <summary>
    /// Representa un periodo académico dentro del sistema.
    /// Los periodos se utilizan para organizar las ofertas de cursos 
    /// dentro de rangos de fechas definidos.
    /// </summary>
    [Table("Periods")]
    public class Periods
    {
        /// <summary>
        /// Identificador único del periodo académico.
        /// </summary>
        public Guid Id { get; set; } // CAMBIADO de int a Guid

        /// <summary>
        /// Nombre del periodo académico (Ej: "2025 - I", "Segundo Semestre 2024").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de inicio del periodo académico.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Fecha de finalización del periodo académico.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Indica si el periodo está activo.
        /// Solo un periodo debería estar activo a la vez.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Fecha en la que se registró el periodo en el sistema.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si el registro ha sido eliminado de forma lógica (soft delete).
        /// </summary>
        public bool IsSoftDeleted { get; set; }

        /// <summary>
        /// Lista de ofertas de cursos que pertenecen a este periodo académico.
        /// </summary>
        public ICollection<CourseOfferings> CourseOfferings { get; set; } = new List<CourseOfferings>();
    }
}
