using System.ComponentModel.DataAnnotations;
using static System.Collections.Specialized.BitVector32;

namespace Control_De_Tareas.Models
{
    public class AnnouncementVm
    {
        public int Id { get; set; }

    [Required(ErrorMessage = "Debe seleccionar una oferta de curso")]
    [Display(Name = "Oferta de Curso")]
    public int CourseOfferingId { get; set; }

    [Required(ErrorMessage = "El título es requerido")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 200 caracteres")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000, ErrorMessage = "El contenido no puede exceder 5000 caracteres")]
    [Display(Name = "Contenido")]
    [DataType(DataType.MultilineText)]
    public string? Body { get; set; }

    [Display(Name = "Fecha de Publicación")]
    [DataType(DataType.DateTime)]
    public DateTime PostedAt { get; set; } = DateTime.Now;

    [Display(Name = "Publicado Por")]
    public Guid PostedBy { get; set; }

    // ========== Propiedades adicionales para vistas ==========

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
    /// Sección del curso.
    /// </summary>
    [Display(Name = "Sección")]
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Período académico.
    /// </summary>
    [Display(Name = "Período")]
    public string PeriodName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del autor del anuncio.
    /// </summary>
    [Display(Name = "Publicado por")]
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Email del autor.
    /// </summary>
    [Display(Name = "Email")]
    public string AuthorEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo de la oferta.
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
    /// Tiempo transcurrido desde la publicación (ej: "hace 2 horas").
    /// </summary>
    [Display(Name = "Publicado")]
    public string TimeAgo
    {
        get
        {
            var timeSpan = DateTime.Now - PostedAt;

            if (timeSpan.TotalMinutes < 1)
                return "Hace un momento";
            if (timeSpan.TotalMinutes < 60)
                return $"Hace {(int)timeSpan.TotalMinutes} minuto{((int)timeSpan.TotalMinutes > 1 ? "s" : "")}";
            if (timeSpan.TotalHours < 24)
                return $"Hace {(int)timeSpan.TotalHours} hora{((int)timeSpan.TotalHours > 1 ? "s" : "")}";
            if (timeSpan.TotalDays < 7)
                return $"Hace {(int)timeSpan.TotalDays} día{((int)timeSpan.TotalDays > 1 ? "s" : "")}";
            if (timeSpan.TotalDays < 30)
                return $"Hace {(int)(timeSpan.TotalDays / 7)} semana{((int)(timeSpan.TotalDays / 7) > 1 ? "s" : "")}";
            if (timeSpan.TotalDays < 365)
                return $"Hace {(int)(timeSpan.TotalDays / 30)} mes{((int)(timeSpan.TotalDays / 30) > 1 ? "es" : "")}";

            return PostedAt.ToString("dd/MM/yyyy");
        }
    }

    /// <summary>
    /// Vista previa del contenido (primeros 150 caracteres).
    /// </summary>
    [Display(Name = "Vista Previa")]
    public string BodyPreview
    {
        get
        {
            if (string.IsNullOrEmpty(Body))
                return "Sin contenido";

            if (Body.Length <= 150)
                return Body;

            return Body.Substring(0, 150) + "...";
        }
    }

    /// <summary>
    /// Indica si el anuncio tiene contenido.
    /// </summary>
    public bool HasBody => !string.IsNullOrWhiteSpace(Body);
}
}