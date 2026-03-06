using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class TareaVm
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaEntrega { get; set; }
        public decimal MaxScore { get; set; }
    }
    public class TareaEditVm
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        public string Title { get; set; }

        [StringLength(2000, ErrorMessage = "La descripción no puede exceder 2000 caracteres")]
        public string Description { get; set; }

        [Required(ErrorMessage = "La fecha límite es requerida")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "La puntuación máxima es requerida")]
        [Range(0, 1000, ErrorMessage = "La puntuación debe estar entre 0 y 1000")]
        public decimal MaxScore { get; set; }

        // Estos campos no se editan, pero los necesitamos
        public Guid CourseOfferingId { get; set; }
        public Guid CreatedBy { get; set; }
    }
}

