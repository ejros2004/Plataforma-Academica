using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class CourseOfferingVm : IValidatableObject
    {
        public Guid Id { get; set; } // CAMBIADO de int a Guid

        [Required(ErrorMessage = "Debe seleccionar un curso")]
        [Display(Name = "Curso")]
        public Guid CourseId { get; set; } // CAMBIADO de int a Guid

        [Required(ErrorMessage = "Debe asignar un profesor")]
        [Display(Name = "Profesor")]
        public Guid ProfessorId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un período")]
        [Display(Name = "Período Académico")]
        public Guid PeriodId { get; set; } // CAMBIADO de int a Guid

        [StringLength(50, ErrorMessage = "La sección no puede exceder 50 caracteres")]
        [Display(Name = "Sección")]
        public string? Section { get; set; }

        [Display(Name = "Fecha de Creación")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "¿Está Activo?")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Nombre del Curso")]
        public string CourseName { get; set; } = string.Empty;

        [Display(Name = "Código del Curso")]
        public string CourseCode { get; set; } = string.Empty;

        [Display(Name = "Profesor")]
        public string ProfessorName { get; set; } = string.Empty;

        [Display(Name = "Email del Profesor")]
        public string ProfessorEmail { get; set; } = string.Empty;

        [Display(Name = "Período")]
        public string PeriodName { get; set; } = string.Empty;

        [Display(Name = "Inicio del Período")]
        [DataType(DataType.Date)]
        public DateTime? PeriodStartDate { get; set; }

        [Display(Name = "Fin del Período")]
        [DataType(DataType.Date)]
        public DateTime? PeriodEndDate { get; set; }

        [Display(Name = "Estudiantes Inscritos")]
        public int EnrolledStudentsCount { get; set; }

        [Display(Name = "Tareas Asignadas")]
        public int TasksCount { get; set; }

        [Display(Name = "Anuncios")]
        public int AnnouncementsCount { get; set; }

        [Display(Name = "Capacidad Máxima")]
        [Range(0, 500, ErrorMessage = "La capacidad debe estar entre 0 y 500")]
        public int? MaxCapacity { get; set; }

        [Display(Name = "Cupos Disponibles")]
        public bool HasAvailableSpots
        {
            get
            {
                if (!MaxCapacity.HasValue)
                    return true;

                return EnrolledStudentsCount < MaxCapacity.Value;
            }
        }

        [Display(Name = "Oferta")]
        public string FullDisplayName
        {
            get
            {
                var display = CourseName;
                if (!string.IsNullOrEmpty(Section))
                    display += $" - Sección {Section}";
                if (!string.IsNullOrEmpty(PeriodName))
                    display += $" ({PeriodName})";
                return display;
            }
        }

        [Display(Name = "Estado")]
        public string Status
        {
            get
            {
                if (!IsActive)
                    return "Inactivo";

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
            set { }
        }

        public bool CanEnroll
        {
            get
            {
                return IsActive && HasAvailableSpots && Status == "En Curso";
            }
        }

        [Display(Name = "% Ocupación")]
        public decimal OccupancyPercentage
        {
            get
            {
                if (!MaxCapacity.HasValue || MaxCapacity.Value == 0)
                    return 0;

                return Math.Round((decimal)EnrolledStudentsCount / MaxCapacity.Value * 100, 2);
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            // 1. Validar que el ProfessorId no sea vacío
            if (ProfessorId == Guid.Empty)
            {
                errors.Add(new ValidationResult(
                    "Debe seleccionar un profesor válido",
                    new[] { nameof(ProfessorId) }
                ));
            }

            // 2. Validar que si hay capacidad máxima, los inscritos no la excedan
            if (MaxCapacity.HasValue && EnrolledStudentsCount > MaxCapacity.Value)
            {
                errors.Add(new ValidationResult(
                    $"La cantidad de estudiantes inscritos ({EnrolledStudentsCount}) no puede exceder la capacidad máxima ({MaxCapacity.Value})",
                    new[] { nameof(EnrolledStudentsCount) }
                ));
            }

            // 3. Validar que la sección no esté vacía si es requerida
            if (string.IsNullOrWhiteSpace(Section))
            {
                errors.Add(new ValidationResult(
                    "La sección es requerida",
                    new[] { nameof(Section) }
                ));
            }

            // 4. Validar que el período esté activo (si aplica)
            if (PeriodStartDate.HasValue && PeriodEndDate.HasValue)
            {
                if (PeriodEndDate.Value < DateTime.Now.Date && IsActive)
                {
                    errors.Add(new ValidationResult(
                        "No se puede activar una oferta en un período que ya finalizó",
                        new[] { nameof(IsActive) }
                    ));
                }
            }

            return errors;
        }
    }
}