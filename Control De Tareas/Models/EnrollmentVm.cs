using System;
using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    /// <summary>
    /// Modelo para transferir datos de inscripciones entre las capas de la aplicación.
    /// </summary>
    public class EnrollmentVm
    {
        public Guid Id { get; set; } // CAMBIADO de int a Guid

        [Required(ErrorMessage = "Debe seleccionar una oferta de curso")]
        [Display(Name = "Oferta de Curso")]
        public Guid CourseOfferingId { get; set; } // CAMBIADO de int a Guid

        [Required(ErrorMessage = "Debe seleccionar un estudiante")]
        [Display(Name = "Estudiante")]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "La fecha de inscripción es requerida")]
        [Display(Name = "Fecha de Inscripción")]
        [DataType(DataType.DateTime)]
        public DateTime EnrolledAt { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El estado es requerido")]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Status { get; set; } = "Active";

        // ========== Propiedades adicionales para vistas ==========

        /// <summary>
        /// Nombre del estudiante inscrito.
        /// </summary>
        [Display(Name = "Estudiante")]
        public string StudentName { get; set; } = string.Empty;

        /// <summary>
        /// Email del estudiante.
        /// </summary>
        [Display(Name = "Email")]
        public string StudentEmail { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del curso.
        /// </summary>
        [Display(Name = "Curso")]
        public string CourseName { get; set; } = string.Empty;

        /// <summary>
        /// Código del curso.
        /// </summary>
        [Display(Name = "Código")]
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del profesor asignado.
        /// </summary>
        [Display(Name = "Profesor")]
        public string ProfessorName { get; set; } = string.Empty;

        /// <summary>
        /// Período académico.
        /// </summary>
        [Display(Name = "Período")]
        public string PeriodName { get; set; } = string.Empty;

        /// <summary>
        /// Sección del curso.
        /// </summary>
        [Display(Name = "Sección")]
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de inicio del período.
        /// </summary>
        [Display(Name = "Inicio del Período")]
        [DataType(DataType.Date)]
        public DateTime? PeriodStartDate { get; set; }

        /// <summary>
        /// Fecha de fin del período.
        /// </summary>
        [Display(Name = "Fin del Período")]
        [DataType(DataType.Date)]
        public DateTime? PeriodEndDate { get; set; }

        /// <summary>
        /// Nombre completo de la oferta de curso.
        /// Formato: "MAT101 - Matemáticas I (Sección A) - 2025-I"
        /// </summary>
        [Display(Name = "Curso Completo")]
        public string FullCourseName
        {
            get
            {
                var display = $"{CourseCode} - {CourseName}";
                if (!string.IsNullOrEmpty(Section))
                    display += $" (Sección {Section})";
                if (!string.IsNullOrEmpty(PeriodName))
                    display += $" - {PeriodName}";
                return display;
            }
        }

        /// <summary>
        /// Estado actual del curso basado en fechas.
        /// </summary>
        [Display(Name = "Estado del Curso")]
        public string CourseStatus
        {
            get
            {
                if (Status == "Dropped")
                    return "Retirado";

                if (Status == "Completed")
                    return "Completado";

                if (!PeriodStartDate.HasValue || !PeriodEndDate.HasValue)
                    return "Pendiente";

                var today = DateTime.Now.Date;
                if (today < PeriodStartDate.Value.Date)
                    return "Próximo";
                else if (today >= PeriodStartDate.Value.Date && today <= PeriodEndDate.Value.Date)
                    return "En Curso";
                else
                    return "Finalizado";
            }
        }

        /// <summary>
        /// Indica si el estudiante puede retirarse del curso.
        /// </summary>
        public bool CanWithdraw
        {
            get
            {
                return Status == "Active" && CourseStatus == "En Curso";
            }
        }

        /// <summary>
        /// Color del badge según el estado.
        /// </summary>
        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    "Active" => "success",
                    "Dropped" => "danger",
                    "Completed" => "primary",
                    _ => "secondary"
                };
            }
        }
    }
}