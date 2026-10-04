using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.EntityFrameworkCore;

using LearningBff.Entities;

namespace LearningBff.Data;

public class LearningBffDbContext :
    AbpDbContext<LearningBffDbContext>,
    IIdentityDbContext,
    ISettingManagementDbContext,
    IPermissionManagementDbContext,
    IFeatureManagementDbContext,
    IBackgroundJobsDbContext
{
    public const string DbTablePrefix = "App";
    public const string DbSchema = null;

    // App entities
    public DbSet<Answer> Answers { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<EnrollmentSubject> EnrollmentSubjects { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }
    public DbSet<ExamResultAnswer> ExamResultAnswers { get; set; }
    public DbSet<LearningProgress> LearningProgesses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Subject> Subjects { get; set; }

    // IIdentityDbContext
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // ISettingManagementDbContext
    public DbSet<Setting> Settings { get; set; }
    public DbSet<SettingDefinitionRecord> SettingDefinitionRecords { get; set; }

    // IPermissionManagementDbContext
    public DbSet<PermissionGroupDefinitionRecord> PermissionGroups { get; set; }
    public DbSet<PermissionDefinitionRecord> Permissions { get; set; }
    public DbSet<PermissionGrant> PermissionGrants { get; set; }
    public DbSet<ResourcePermissionGrant> ResourcePermissionGrants { get; set; }

    // IFeatureManagementDbContext
    public DbSet<FeatureGroupDefinitionRecord> FeatureGroups { get; set; }
    public DbSet<FeatureDefinitionRecord> Features { get; set; }
    public DbSet<FeatureValue> FeatureValues { get; set; }

    // IBackgroundJobsDbContext
    public DbSet<BackgroundJobRecord> BackgroundJobs { get; set; }

    public LearningBffDbContext(DbContextOptions<LearningBffDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigurePermissionManagement();
        builder.ConfigureBlobStoring();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();

        /* Configure your own entities here */
        builder.Entity<Answer>(b =>
        {
            b.ToTable(DbTablePrefix + "Answers", DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Text).IsRequired();
        });

        builder.Entity<Chapter>(b =>
        {
            b.ToTable(DbTablePrefix + "Chapters", DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired();
            b.HasMany(x => x.Lessons).WithOne(x => x.Chapter).HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Exams).WithOne(x => x.Chapter).HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade);

        });
        builder.Entity<EnrollmentSubject>(b =>
        {
            b.ToTable(DbTablePrefix + "EnrollmentSubjects", DbSchema);
            b.ConfigureByConvention();
            b.Ignore(x => x.StudentId);
            b.HasIndex(x => new { x.UserId, x.SubjectId }).IsUnique();
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Subject).WithMany(x => x.EnrollmentSubjects).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Exam>(b =>
        {
            b.ToTable(DbTablePrefix + "Exams", DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(1024);
            b.HasOne(x => x.Chapter).WithMany(x => x.Exams).HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Subject).WithMany(x => x.Exams).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.ExamQuestions).WithOne(x => x.Exam).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.ExamResults).WithOne(x => x.Exam).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ExamQuestion>(b =>
        {
            b.ToTable(DbTablePrefix + "ExamQuestions", DbSchema);
            b.ConfigureByConvention();
            b.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Exam).WithMany(x => x.ExamQuestions).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ExamResult>(b =>
        {
            b.ToTable(DbTablePrefix + "ExamResults", DbSchema);
            b.ConfigureByConvention();
            b.Ignore(x => x.StudentId);
            b.Property(x => x.Status).HasConversion<int>();
            b.HasOne(x => x.Exam).WithMany(x => x.ExamResults).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.ExamResultAnswers).WithOne(x => x.ExamResult).HasForeignKey(x => x.ExamResultId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ExamResultAnswer>(b =>
        {
            b.ToTable(DbTablePrefix + "ExamResultAnswers", DbSchema);
            b.ConfigureByConvention();
            b.HasOne(x => x.ExamResult).WithMany(x => x.ExamResultAnswers).HasForeignKey(x => x.ExamResultId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Question).WithMany(x => x.ExamResultAnswers).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Answer).WithMany(x => x.ExamResultAnswers).HasForeignKey(x => x.AnswerId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LearningProgress>(b =>
        {
            b.ToTable(DbTablePrefix + "LearningProgress", DbSchema);
            b.ConfigureByConvention();
            b.Ignore(x => x.StudentId);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Lesson).WithMany(x => x.LearningProgresses).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Lesson>(b =>
        {
            b.ToTable(DbTablePrefix + "Lessons", DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired();
            b.HasOne(x => x.Chapter).WithMany(x => x.Lessons).HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.LearningProgresses).WithOne(x => x.Lesson).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Question>(b =>
        {
            b.ToTable(DbTablePrefix + "Questions", DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired();
            b.Property(x => x.Type).HasConversion<int>();
            b.Property(x => x.Difficulty).HasConversion<int>();
            b.HasMany(x => x.ExamQuestions).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.ExamResultAnswers).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Subject).WithMany(x => x.Questions).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Answers).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Subject>(b =>
        {
            b.ToTable(DbTablePrefix + "Subjects", DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired();
            b.HasMany(x => x.Questions).WithOne(x => x.Subject).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Exams).WithOne(x => x.Subject).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(b => b.Chapters).WithOne(x => x.Subject).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.EnrollmentSubjects).WithOne(x => x.Subject).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Teachers)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "AppSubjectTeachers",
                    j => j.HasOne<IdentityUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Subject>().WithMany().HasForeignKey("SubjectId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.ToTable(DbTablePrefix + "SubjectTeachers", DbSchema);
                        j.HasKey("SubjectId", "UserId");
                    });
        });
    }
}

