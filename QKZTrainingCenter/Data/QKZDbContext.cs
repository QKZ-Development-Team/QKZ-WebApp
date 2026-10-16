using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Models;

namespace QKZTrainingCenter.Data
{
    public class QKZDbContext : DbContext
    {
        public QKZDbContext(DbContextOptions<QKZDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }

        public DbSet<ComboPackage> ComboPackages { get; set; }
        public DbSet<ComboPackageCourse> ComboPackageCourses { get; set; }
        public DbSet<QKZUser> Users { get; set; }

        public DbSet<Student> Students { get; set; }

        public DbSet<Registration> Registrations { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<StudyMaterial> StudyMaterials { get; set; }
       

        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<DeliveryMode> DeliveryModes { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public async Task<bool> TestConnectionAsync()
        {
            return await Database.CanConnectAsync();
        }
    }
}