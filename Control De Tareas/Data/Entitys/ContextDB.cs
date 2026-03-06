using Microsoft.EntityFrameworkCore;
using Control_De_Tareas.Data.Entitys;
using Control_De_Tareas.Data.Configurations;

namespace Control_De_Tareas.Data
{
    public class ContextDB : DbContext
    {
        public ContextDB(DbContextOptions<ContextDB> options) : base(options)
        {
        }

        // ========== DbSets CON NOMBRES ORIGINALES ==========
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Tareas> Tareas { get; set; }
        public DbSet<Submissions> Submissions { get; set; }
        public DbSet<Courses> Courses { get; set; }
        public DbSet<Module> Module { get; set; }
        public DbSet<ModuleGroup> ModuleGroup { get; set; }
        public DbSet<RoleModules> RoleModules { get; set; }
        public DbSet<UserRoles> UserRoles { get; set; }
        public DbSet<Periods> Periods { get; set; }
        public DbSet<CourseOfferings> CourseOfferings { get; set; }
        public DbSet<Enrollments> Enrollments { get; set; }
        public DbSet<SubmissionFiles> SubmissionFiles { get; set; }
        public DbSet<Grades> Grades { get; set; }
        public DbSet<Announcements> Announcements { get; set; }
        public DbSet<AuditLogs> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== CONFIGURACIONES ==========
            modelBuilder.ApplyConfiguration(new UsersConfig());
            modelBuilder.ApplyConfiguration(new RolesConfig());
            modelBuilder.ApplyConfiguration(new UserRolesConfig());
            modelBuilder.ApplyConfiguration(new CoursesConfiguration());
            modelBuilder.ApplyConfiguration(new TareasConfig());
            modelBuilder.ApplyConfiguration(new SubmissionsConfig());
            modelBuilder.ApplyConfiguration(new ModuleGroupConfig());
            modelBuilder.ApplyConfiguration(new RoleModulesConfig());
            modelBuilder.ApplyConfiguration(new ModuleConfig());
            modelBuilder.ApplyConfiguration(new AnnouncementsConfig());
            modelBuilder.ApplyConfiguration(new AuditLogsConfig());
            modelBuilder.ApplyConfiguration(new GradesConfig());
            modelBuilder.ApplyConfiguration(new SubmissionFilesConfig());
            modelBuilder.ApplyConfiguration(new CourseOfferingsConfig());
            modelBuilder.ApplyConfiguration(new EnrollmentsConfig());
            modelBuilder.ApplyConfiguration(new PeriodsConfig());

            // ========== RELACIONES ADICIONALES ==========

            // Users -> Tareas (CreatedBy)
            modelBuilder.Entity<Tareas>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Submissions -> Grades
            modelBuilder.Entity<Submissions>()
                .HasMany(s => s.Grades)
                .WithOne(g => g.Submission)
                .HasForeignKey(g => g.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Submissions -> SubmissionFiles
            modelBuilder.Entity<Submissions>()
                .HasMany(s => s.SubmissionFiles)
                .WithOne(sf => sf.Submission)
                .HasForeignKey(sf => sf.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // CourseOfferings -> Tareas
            modelBuilder.Entity<CourseOfferings>()
                .HasMany(co => co.Tareas)
                .WithOne(t => t.CourseOffering)
                .HasForeignKey(t => t.CourseOfferingId)
                .OnDelete(DeleteBehavior.Restrict);

            // CourseOfferings -> Announcements
            modelBuilder.Entity<CourseOfferings>()
                .HasMany(co => co.Announcements)
                .WithOne(a => a.CourseOffering)
                .HasForeignKey(a => a.CourseOfferingId)
                .OnDelete(DeleteBehavior.Restrict);

            // CourseOfferings -> Enrollments
            modelBuilder.Entity<CourseOfferings>()
                .HasMany(co => co.Enrollments)
                .WithOne(e => e.CourseOffering)
                .HasForeignKey(e => e.CourseOfferingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Announcements -> PostedByUser
            modelBuilder.Entity<Announcements>()
                .HasOne(a => a.PostedByUser)
                .WithMany()
                .HasForeignKey(a => a.PostedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Grades -> Grader
            modelBuilder.Entity<Grades>()
                .HasOne(g => g.Grader)
                .WithMany()
                .HasForeignKey(g => g.GraderId)
                .OnDelete(DeleteBehavior.Restrict);

            // AuditLogs -> User
            modelBuilder.Entity<AuditLogs>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ========== ÍNDICES ÚNICOS ==========

            // CourseOfferings: Curso + Periodo + Sección debe ser único
            modelBuilder.Entity<CourseOfferings>()
                .HasIndex(co => new { co.CourseId, co.PeriodId, co.Section })
                .IsUnique()
                .HasFilter("[IsSoftDeleted] = 0");

            // Enrollments: Estudiante no puede estar inscrito dos veces en la misma oferta
            modelBuilder.Entity<Enrollments>()
                .HasIndex(e => new { e.CourseOfferingId, e.StudentId })
                .IsUnique()
                .HasFilter("[IsSoftDeleted] = 0");

            // Users: Email único
            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasFilter("[IsSoftDeleted] = 0");

            // Courses: Código único
            modelBuilder.Entity<Courses>()
                .HasIndex(c => c.Code)
                .IsUnique()
                .HasFilter("[IsSoftDeleted] = 0");

            // ========== CONFIGURACIÓN DELETE BEHAVIOR POR DEFECTO ==========

            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                if (relationship.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    relationship.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }
}