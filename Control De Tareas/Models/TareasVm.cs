using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class TareasVm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El título es requerido")]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int CourseId { get; set; }
        public decimal MaxScore { get; set; }
    }
}