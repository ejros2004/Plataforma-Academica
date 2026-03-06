using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class PeriodVm
    {
        public Guid Id { get; set; } // CAMBIADO de int a Guid

        [Required(ErrorMessage = "El nombre del período es requerido")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
        [Display(Name = "Nombre del Período")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "La fecha de fin es requerida")]
        [Display(Name = "Fecha de Fin")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "¿Está Activo?")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Fecha de Creación")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Cursos Ofertados")]
        public int CourseOfferingsCount { get; set; }

        [Display(Name = "Estado")]
        public string Status
        {
            get
            {
                var today = DateTime.Now.Date;
                if (today < StartDate.Date)
                    return "Próximo";
                else if (today >= StartDate.Date && today <= EndDate.Date)
                    return "En Curso";
                else
                    return "Finalizado";
            }
        }

        [Display(Name = "Duración (días)")]
        public int DurationInDays
        {
            get
            {
                return (EndDate - StartDate).Days;
            }
        }

        public bool CanBeEdited
        {
            get
            {
                return CourseOfferingsCount == 0 && DateTime.Now.Date <= EndDate.Date;
            }
        }
    }
}