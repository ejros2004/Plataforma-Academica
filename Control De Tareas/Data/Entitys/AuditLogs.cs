using System.ComponentModel.DataAnnotations.Schema;

namespace Control_De_Tareas.Data.Entitys
{
    [Table("AuditLogs")]
    public class AuditLogs
    {
        public long Id { get; set; } // SE MANTIENE como long (no es FK)
        public Guid? UserId { get; set; }
        public string? Action { get; set; }
        public string? Entity { get; set; }
        public Guid? EntityId { get; set; } // CAMBIADO de int? a Guid?
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Details { get; set; }
        public bool IsSoftDeleted { get; set; }
        public Users? User { get; set; }
    }
}