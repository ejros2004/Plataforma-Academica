using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    /// <summary>
    /// Representa una oferta de curso dentro de un periodo académico.
    /// Una oferta de curso es una combinación entre un curso base,
    /// el profesor asignado, el periodo y la sección en la que se impartirá.
    /// </summary>
    [Table("CourseOfferings")]
    public class CourseOfferings
    {
        /// <summary>
        /// Identificador único de la oferta de curso.
        /// </summary>
        public Guid Id { get; set; } // CAMBIADO de int a Guid

        /// <summary>
        /// Identificador del curso base asociado.
        /// </summary>
        public Guid CourseId { get; set; } // CAMBIADO de int a Guid

        /// <summary>
        /// Identificador del profesor asignado a esta oferta.
        /// </summary>
        public Guid ProfessorId { get; set; }

        /// <summary>
        /// Identificador del periodo académico en el cual se impartirá este curso.
        /// </summary>
        public Guid PeriodId { get; set; } // CAMBIADO de int a Guid

        /// <summary>
        /// Sección del curso (ejemplo: A1, B, C01).
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// Fecha de creación del registro.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si la oferta está activa para visualización o matrícula.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indica si el registro ha sido marcado como eliminado (borrado lógico).
        /// </summary>
        public bool IsSoftDeleted { get; set; }

        /// <summary>
        /// Relación con el curso base al que pertenece esta oferta.
        /// </summary>
        public Courses Course { get; set; } = null!;

        /// <summary>
        /// Relación con el profesor asignado a impartir esta oferta.
        /// </summary>
        public Users Professor { get; set; } = null!;

        /// <summary>
        /// Relación con el periodo académico correspondiente.
        /// </summary>
        public Periods Period { get; set; } = null!;

        /// <summary>
        /// Colección de inscripciones de estudiantes asociadas a esta oferta.
        /// </summary>
        public ICollection<Enrollments> Enrollments { get; set; } = new List<Enrollments>();

        /// <summary>
        /// Colección de tareas asignadas dentro de esta oferta de curso.
        /// </summary>
        public ICollection<Tareas> Tareas { get; set; } = new List<Tareas>();

        /// <summary>
        /// Colección de anuncios publicados dentro del curso.
        /// </summary>
        public ICollection<Announcements> Announcements { get; set; } = new List<Announcements>();
    }
}
