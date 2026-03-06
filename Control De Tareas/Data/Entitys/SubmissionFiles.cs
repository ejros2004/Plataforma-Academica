using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    [Table("SubmissionFiles")]
    public class SubmissionFiles
    {
        public Guid Id { get; set; } // CAMBIADO de int a Guid
        public Guid SubmissionId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public bool IsSoftDeleted { get; set; }
        public Submissions Submission { get; set; } = null!;
    }
}