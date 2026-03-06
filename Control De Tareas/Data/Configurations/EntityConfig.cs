using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Control_De_Tareas.Data.Entitys;

namespace Control_De_Tareas.Data.Configurations
{
    public class TareasConfig : IEntityTypeConfiguration<Tareas>
    {
        public void Configure(EntityTypeBuilder<Tareas> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(t => t.Description)
                   .HasMaxLength(1000);

            builder.Property(t => t.DueDate)
                   .IsRequired();

            builder.Property(t => t.MaxScore)
                   .IsRequired()
                   .HasPrecision(18, 2);
        }
    }
    public class CoursesConfiguration : IEntityTypeConfiguration<Courses>
    {
        public void Configure(EntityTypeBuilder<Courses> builder)
        {
            builder.ToTable("Courses");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                   .IsRequired()
                   .HasMaxLength(10);

            builder.Property(c => c.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(c => c.Description)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(c => c.CreatedAt)
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(c => c.IsActive)
                   .HasDefaultValue(true);

            builder.Property(c => c.IsSoftDeleted)
                   .HasDefaultValue(false);

            // AGREGAR ESTAS RELACIONES:
            builder.HasMany(c => c.CourseOfferings)
                   .WithOne(co => co.Course)
                   .HasForeignKey(co => co.CourseId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class RolesConfig : IEntityTypeConfiguration<Roles>
    {
        public void Configure(EntityTypeBuilder<Roles> builder)
        {
            builder.HasKey(r => r.RoleId);

            builder.Property(r => r.RoleName)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(r => r.Description)
                   .HasMaxLength(200)
                   .IsRequired(false);

            builder.Property(r => r.CreateAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.HasMany(r => r.RoleModules)
                   .WithOne(rm => rm.Role)
                   .HasForeignKey(rm => rm.RoleId);

            builder.HasMany(r => r.Users)
                   .WithOne(u => u.Rol)
                   .HasForeignKey(u => u.RolId);
        }
    }

    public class UsersConfig : IEntityTypeConfiguration<Users>
    {
        public void Configure(EntityTypeBuilder<Users> builder)
        {
            builder.HasKey(u => u.UserId);

            builder.Property(u => u.UserName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.Email)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.HasIndex(u => u.Email)
                   .IsUnique();

            builder.Property(u => u.PasswordHash)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(u => u.Instructor)
                   .HasMaxLength(100);

            builder.Property(u => u.CreateAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(u => u.IsSoftDeleted)
                   .HasDefaultValue(false);

            builder.HasMany(u => u.UserRoles)
                   .WithOne(ur => ur.User)
                   .HasForeignKey(ur => ur.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Rol)
                   .WithMany(r => r.Users)
                   .HasForeignKey(u => u.RolId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class UserRolesConfig : IEntityTypeConfiguration<UserRoles>
    {
        public void Configure(EntityTypeBuilder<UserRoles> builder)
        {
            builder.HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.Property(ur => ur.CreatAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.HasOne(ur => ur.User)
                   .WithMany(u => u.UserRoles)
                   .HasForeignKey(ur => ur.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ur => ur.Role)
                   .WithMany(r => r.UserRoles)
                   .HasForeignKey(ur => ur.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(ur => new { ur.UserId, ur.RoleId });
        }
    }

    public class SubmissionsConfig : IEntityTypeConfiguration<Submissions>
    {
        public void Configure(EntityTypeBuilder<Submissions> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.TaskId)
                   .IsRequired();

            builder.Property(s => s.StudentId)
                   .IsRequired();

            builder.Property(s => s.SubmittedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(s => s.CurrentGrade)
                   .HasPrecision(5, 2)
                   .IsRequired();

            builder.Property(s => s.Comments)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            builder.Property(s => s.IsSoftDeleted)
                   .HasDefaultValue(false);

            builder.HasOne(s => s.Task)
                   .WithMany(t => t.Submissions)
                   .HasForeignKey(s => s.TaskId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Student)
                   .WithMany()
                   .HasForeignKey(s => s.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.TaskId);
            builder.HasIndex(s => s.StudentId);
            builder.HasIndex(s => s.SubmittedAt);
        }
    }

    public class ModuleGroupConfig : IEntityTypeConfiguration<ModuleGroup>
    {
        public void Configure(EntityTypeBuilder<ModuleGroup> builder)
        {
            builder.HasKey(mg => mg.GroupModuleId);
            builder.Property(mg => mg.Description).IsRequired().HasMaxLength(200);
            builder.Property(mg => mg.CreateAt).IsRequired().HasDefaultValueSql("GETDATE()");
            builder.HasMany(mg => mg.Modules).WithOne(m => m.ModuloAgrupado).HasForeignKey(m => m.ModuloAgrupadoId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ModuleConfig : IEntityTypeConfiguration<Module>
    {
        public void Configure(EntityTypeBuilder<Module> builder)
        {
            builder.HasKey(m => m.ModuleId);
            builder.HasMany(m => m.RoleModules).WithOne(rm => rm.Module).HasForeignKey(rm => rm.ModuleId);
        }
    }

    public class RoleModulesConfig : IEntityTypeConfiguration<RoleModules>
    {
        public void Configure(EntityTypeBuilder<RoleModules> builder)
        {
            builder.HasKey(mr => mr.ModuleRoleId);

            builder.HasOne(rm => rm.Role)
                   .WithMany(r => r.RoleModules)
                   .HasForeignKey(rm => rm.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rm => rm.Module)
                   .WithMany(m => m.RoleModules)
                   .HasForeignKey(rm => rm.ModuleId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class AuditLogsConfig : IEntityTypeConfiguration<AuditLogs>
    {
        public void Configure(EntityTypeBuilder<AuditLogs> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Action)
                   .HasMaxLength(100);

            builder.Property(a => a.Entity)
                   .HasMaxLength(100);

            builder.Property(a => a.EntityId) // ← Ya usa Guid?
                   .IsRequired(false);

            builder.Property(a => a.Details)
                   .HasMaxLength(2000);

            builder.Property(a => a.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(a => a.IsSoftDeleted)
                   .HasDefaultValue(false);

            builder.HasOne(a => a.User)
                   .WithMany()
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(a => a.UserId);
            builder.HasIndex(a => a.CreatedAt);
        }
    }

    public class GradesConfig : IEntityTypeConfiguration<Grades>
    {
        public void Configure(EntityTypeBuilder<Grades> builder)
        {
            builder.HasKey(g => g.Id); // ← Ya usa Guid

            builder.Property(g => g.Score)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(g => g.Feedback)
                   .HasMaxLength(2000);

            builder.Property(g => g.GradedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(g => g.IsSoftDeleted)
                   .HasDefaultValue(false);

            builder.HasOne(g => g.Submission)
                   .WithMany(s => s.Grades)
                   .HasForeignKey(g => g.SubmissionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(g => g.Grader)
                   .WithMany()
                   .HasForeignKey(g => g.GraderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(g => g.SubmissionId);
            builder.HasIndex(g => g.GraderId);
        }
    }

    public class SubmissionFilesConfig : IEntityTypeConfiguration<SubmissionFiles>
    {
        public void Configure(EntityTypeBuilder<SubmissionFiles> builder)
        {
            builder.HasKey(sf => sf.Id); // ← Ya usa Guid

            builder.Property(sf => sf.FilePath)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(sf => sf.FileName)
                   .HasMaxLength(255);

            builder.Property(sf => sf.UploadedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(sf => sf.IsSoftDeleted)
                   .HasDefaultValue(false);

            builder.HasOne(sf => sf.Submission)
                   .WithMany(s => s.SubmissionFiles)
                   .HasForeignKey(sf => sf.SubmissionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(sf => sf.SubmissionId);
        }
    }

    public class AnnouncementsConfig : IEntityTypeConfiguration<Announcements>
    {
        public void Configure(EntityTypeBuilder<Announcements> builder)
        {
            builder.HasKey(a => a.Id); // ← Ya usa Guid

            builder.Property(a => a.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(a => a.Body)
                   .HasMaxLength(2000);

            builder.Property(a => a.PostedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(a => a.PostedBy)
                   .IsRequired();

            builder.Property(a => a.IsSoftDeleted)
                   .HasDefaultValue(false);

            builder.HasOne(a => a.CourseOffering)
                   .WithMany(co => co.Announcements)
                   .HasForeignKey(a => a.CourseOfferingId) // ← Ya usa Guid
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.PostedByUser)
                   .WithMany()
                   .HasForeignKey(a => a.PostedBy)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired();

            builder.HasIndex(a => a.CourseOfferingId);
            builder.HasIndex(a => a.PostedBy);
        }
    }

    public class CourseOfferingsConfig : IEntityTypeConfiguration<CourseOfferings>
    {
        public void Configure(EntityTypeBuilder<CourseOfferings> builder)
        {
            builder.HasKey(co => co.Id);

            builder.Property(co => co.Section)
                   .HasMaxLength(10);

            builder.Property(co => co.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(co => co.IsActive)
                   .HasDefaultValue(true);

            builder.Property(co => co.IsSoftDeleted)
                   .HasDefaultValue(false);

            // RELACIÓN CON COURSE
            builder.HasOne(co => co.Course)
                   .WithMany(c => c.CourseOfferings)
                   .HasForeignKey(co => co.CourseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(co => co.Professor)
                   .WithMany()
                   .HasForeignKey(co => co.ProfessorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(co => co.Period)
                   .WithMany(p => p.CourseOfferings)
                   .HasForeignKey(co => co.PeriodId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(co => co.CourseId);
            builder.HasIndex(co => co.ProfessorId);
            builder.HasIndex(co => co.PeriodId);

            // Índice único para evitar duplicados
            builder.HasIndex(co => new { co.CourseId, co.PeriodId, co.Section })
                   .IsUnique()
                   .HasFilter("[IsSoftDeleted] = 0");
        }
    }

    public class EnrollmentsConfig : IEntityTypeConfiguration<Enrollments>
    {
        public void Configure(EntityTypeBuilder<Enrollments> builder)
        {
            builder.HasKey(e => e.Id); // ← Ya usa Guid

            builder.Property(e => e.EnrolledAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(e => e.Status)
                   .IsRequired()
                   .HasMaxLength(50)
                   .HasDefaultValue("Active");

            builder.Property(e => e.IsSoftDeleted)
                   .HasDefaultValue(false);

            builder.HasOne(e => e.CourseOffering)
                   .WithMany(co => co.Enrollments)
                   .HasForeignKey(e => e.CourseOfferingId) // ← Ya usa Guid
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Student)
                   .WithMany()
                   .HasForeignKey(e => e.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.CourseOfferingId);
            builder.HasIndex(e => e.StudentId);
            builder.HasIndex(e => new { e.CourseOfferingId, e.StudentId })
                   .IsUnique();
        }
    }

    public class PeriodsConfig : IEntityTypeConfiguration<Periods>
    {
        public void Configure(EntityTypeBuilder<Periods> builder)
        {
            builder.HasKey(p => p.Id); // ← Ya usa Guid

            builder.Property(p => p.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(p => p.StartDate)
                   .IsRequired();

            builder.Property(p => p.EndDate)
                   .IsRequired();

            builder.Property(p => p.IsActive)
                   .HasDefaultValue(true);

            builder.Property(p => p.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(p => p.IsSoftDeleted)
                   .HasDefaultValue(false);

            builder.HasIndex(p => p.IsActive);
        }
    }
}