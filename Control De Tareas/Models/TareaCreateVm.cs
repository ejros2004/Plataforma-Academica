using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class TareaCreateVm
    {
        [Required(ErrorMessage = "El ID de Course Offering es requerido")]
        [Display(Name = "Curso")]
        public Guid CourseOfferingId { get; set; } // CAMBIADO de int a Guid

        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        [Display(Name = "Título")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es requerida")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha límite es requerida")]
        [FutureDate(ErrorMessage = "La fecha límite debe ser futura")]
        [Display(Name = "Fecha Límite")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "La puntuación máxima es requerida")]
        [Range(1, 100, ErrorMessage = "La puntuación debe estar entre 1 y 100")]
        [Display(Name = "Puntuación Máxima")]
        public decimal MaxScore { get; set; }
    }

    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
            {
                return date > DateTime.Now;
            }
            return false;
        }
    }
}