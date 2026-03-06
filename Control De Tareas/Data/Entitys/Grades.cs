using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    [Table("Grades")]
    public class Grades
    {
        public Guid Id { get; set; } // CAMBIADO de int a Guid
        public Guid SubmissionId { get; set; }
        public Guid GraderId { get; set; }
        public decimal Score { get; set; }
        public string? Feedback { get; set; }
        public DateTime GradedAt { get; set; } = DateTime.Now;
        public bool IsSoftDeleted { get; set; }
        public Submissions Submission { get; set; } = null!;
        public Users Grader { get; set; } = null!;
    }
}