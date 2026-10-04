using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Identity;

namespace LearningBff.Entities;

public class Subject : FullAuditedAggregateRoot<long>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<IdentityUser> Teachers { get; set; } = [];
    public List<Question> Questions { get; set; } = [];
    public List<Exam> Exams { get; set; } = [];
    public List<Chapter> Chapters { get; set; } = [];
    public List<EnrollmentSubject> EnrollmentSubjects { get; set; } = [];
    public Subject()
    {
    }

    public Subject(string name, string? description = null, bool isActive = true)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
    }
}
