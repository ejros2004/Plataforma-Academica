using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    /// <summary>
    /// Representa la inscripción de un estudiante en una oferta de curso específica.
    /// </summary>
    [Table("Enrollments")]
    public class Enrollments
    {
        /// <summary>
        /// Identificador único de la inscripción.
        /// </summary>
        public Guid Id { get; set; } // CAMBIADO de int a Guid

        /// <summary>
        /// Identificador de la oferta de curso en la que el estudiante está matriculado.
        /// </summary>
        public Guid CourseOfferingId { get; set; } // CAMBIADO de int a Guid

        /// <summary>
        /// Identificador del estudiante inscrito en la oferta de curso.
        /// </summary>
        public Guid StudentId { get; set; }

        /// <summary>
        /// Fecha en la que se realizó la inscripción.
        /// </summary>
        public DateTime EnrolledAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Estado actual de la inscripción (por ejemplo: Active, Dropped, Completed).
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Indica si el registro fue eliminado mediante borrado lógico (soft delete).
        /// </summary>
        public bool IsSoftDeleted { get; set; }

        /// <summary>
        /// Relación con la oferta de curso a la cual pertenece esta inscripción.
        /// </summary>
        public CourseOfferings CourseOffering { get; set; } = null!;

        /// <summary>
        /// Relación con el estudiante inscrito.
        /// </summary>
        public Users Student { get; set; } = null!;
    }
}
