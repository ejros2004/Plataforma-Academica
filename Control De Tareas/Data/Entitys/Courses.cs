// En Data/Entitys/Courses.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    [Table("Courses")]
    public class Courses
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public bool IsSoftDeleted { get; set; }

        public ICollection<CourseOfferings> CourseOfferings { get; set; } = new List<CourseOfferings>();
    }
}