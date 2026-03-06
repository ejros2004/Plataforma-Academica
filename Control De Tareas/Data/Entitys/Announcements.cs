using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    [Table("Announcements")]
    public class Announcements
    {
        public Guid Id { get; set; } // CAMBIADO de int a Guid
        public Guid CourseOfferingId { get; set; } // CAMBIADO de int a Guid
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public DateTime PostedAt { get; set; } = DateTime.Now;
        public Guid PostedBy { get; set; }
        public bool IsSoftDeleted { get; set; }
        public CourseOfferings CourseOffering { get; set; } = null!;
        public Users PostedByUser { get; set; } = null!;
    }
}