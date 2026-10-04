using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LearningBff.Services.Dtos;

public class SubjectDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int QuestionCount { get; set; }
    public int ExamCount { get; set; }
}

public class EnrollmentSubjectDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRegistered { get; set; }
}

public class SubjectFilterDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateSubjectDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateSubjectDto : CreateSubjectDto
{
}
