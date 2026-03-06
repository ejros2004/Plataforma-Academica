using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class FileUploadVm
    {
        [Required(ErrorMessage = "Debe seleccionar un curso")]
        [Display(Name = "Course Offering ID")]
        public int CourseOfferingId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una tarea")]
        [Display(Name = "Task ID")]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un archivo")]
        [Display(Name = "Archivo")]
        public IFormFile File { get; set; }
    }
}
