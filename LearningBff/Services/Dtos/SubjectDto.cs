using System;
using System.Collections.Generic;

namespace LearningBff.Services.Dtos;

public class TeacherSimpleDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string DisplayName => !string.IsNullOrWhiteSpace(Name)
        ? $"{Name} {Surname}".Trim() + $" ({UserName})"
        : UserName;
}

public class SubjectDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int QuestionCount { get; set; }
    public int ExamCount { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreationTime { get; set; }
    public List<TeacherSimpleDto> Teachers { get; set; } = new();
    public List<Guid> TeacherIds => Teachers.ConvertAll(t => t.Id);
}

public class CreateSubjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> TeacherIds { get; set; } = new();
}

public class UpdateSubjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> TeacherIds { get; set; } = new();
}

public class AssignTeachersDto
{
    public long SubjectId { get; set; }
    public List<Guid> TeacherIds { get; set; } = new();
}
