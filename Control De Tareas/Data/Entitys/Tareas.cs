using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    [Table("Tasks")]
    public class Tareas
    {
        public Guid Id { get; set; }
        public Guid CourseOfferingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public Guid CreatedBy { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public bool IsSoftDeleted { get; set; }
        public CourseOfferings CourseOffering { get; set; } = null!;
        public Users CreatedByUser { get; set; } = null!;
        public ICollection<Submissions> Submissions { get; set; } = new List<Submissions>();
    }
}