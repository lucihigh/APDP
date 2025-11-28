using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SIMS.Models;

namespace SIMS.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<FacultyProfile> FacultyProfiles => Set<FacultyProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Explicit converters so DateOnly fields map to SQL Server datetime2 without runtime cast issues
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            dt => DateOnly.FromDateTime(dt));

        var nullableDateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null);

        // Adjust Identity key lengths for SQL Server composite indexes
        builder.Entity<IdentityRole>(b =>
        {
            b.Property(r => r.Id).HasColumnType("nvarchar(128)").HasMaxLength(128);
            b.Property(r => r.NormalizedName).HasColumnType("nvarchar(256)").HasMaxLength(256);
        });
        builder.Entity<IdentityUser>(b =>
        {
            b.Property(u => u.Id).HasColumnType("nvarchar(128)").HasMaxLength(128);
            b.Property(u => u.NormalizedUserName).HasColumnType("nvarchar(256)").HasMaxLength(256);
            b.Property(u => u.NormalizedEmail).HasColumnType("nvarchar(256)").HasMaxLength(256);
        });
        builder.Entity<IdentityUserLogin<string>>(b =>
        {
            b.Property(l => l.LoginProvider).HasColumnType("nvarchar(128)").HasMaxLength(128);
            b.Property(l => l.ProviderKey).HasColumnType("nvarchar(128)").HasMaxLength(128);
        });
        builder.Entity<IdentityUserRole<string>>(b =>
        {
            b.Property(ur => ur.UserId).HasColumnType("nvarchar(128)").HasMaxLength(128);
            b.Property(ur => ur.RoleId).HasColumnType("nvarchar(128)").HasMaxLength(128);
        });
        builder.Entity<IdentityUserToken<string>>(b =>
        {
            b.Property(t => t.UserId).HasColumnType("nvarchar(128)").HasMaxLength(128);
            b.Property(t => t.LoginProvider).HasColumnType("nvarchar(128)").HasMaxLength(128);
            b.Property(t => t.Name).HasColumnType("nvarchar(128)").HasMaxLength(128);
        });

        builder.Entity<Course>()
            .HasIndex(c => c.Code)
            .IsUnique();

        builder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        builder.Entity<FacultyProfile>()
            .HasIndex(f => f.Email)
            .IsUnique();

        builder.Entity<Enrollment>()
            .HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique();

        builder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClassSession>()
            .HasOne(cs => cs.Course)
            .WithMany()
            .HasForeignKey(cs => cs.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClassSession>()
            .Property(cs => cs.StartTime)
            .HasConversion(dateOnlyConverter)
            .HasColumnType("datetime2");

        builder.Entity<ClassSession>()
            .Property(cs => cs.EndTime)
            .HasConversion(dateOnlyConverter)
            .HasColumnType("datetime2");

        builder.Entity<Assessment>()
            .HasOne(a => a.Course)
            .WithMany()
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Assessment>()
            .Property(a => a.DueDate)
            .HasConversion(nullableDateOnlyConverter)
            .HasColumnType("datetime2");

        builder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        builder.Entity<Student>()
            .Property(s => s.DateOfBirth)
            .HasConversion(nullableDateOnlyConverter)
            .HasColumnType("datetime2");

        builder.Entity<FacultyProfile>()
            .Property(f => f.DateOfBirth)
            .HasConversion(nullableDateOnlyConverter)
            .HasColumnType("datetime2");

        builder.Entity<FacultyProfile>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId);
    }
}
