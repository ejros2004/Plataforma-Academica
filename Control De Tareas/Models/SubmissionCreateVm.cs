using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Control_De_Tareas.Models
{
    public class SubmissionCreateVm
    {
        [Required(ErrorMessage = "Debe seleccionar una tarea")]
        [Display(Name = "Tarea")]
        public Guid TaskId { get; set; }

        [Display(Name = "Comentarios adicionales")]
        [MaxLength(1000, ErrorMessage = "Los comentarios no pueden exceder 1000 caracteres")]
        public string Comments { get; set; }

        [Required(ErrorMessage = "Debe subir al menos un archivo")]
        [Display(Name = "Archivos")]
        public List<IFormFile> Files { get; set; }

        // Propiedades NO requeridas (solo para mostrar en GET)
        [Display(Name = "Título de la Tarea")]
        public string TaskTitle { get; set; }

        [Display(Name = "Fecha límite")]
        public DateTime? TaskDueDate { get; set; }
    }
}