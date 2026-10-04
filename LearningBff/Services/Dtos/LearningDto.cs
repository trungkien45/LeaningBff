using System;
using System.Collections.Generic;

namespace LearningBff.Services.Dtos;

public class TeacherDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class SubjectCardDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsEnrolled { get; set; }
    public DateTime? EnrolledAt { get; set; }
    public int TotalChapters { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int ProgressPercentage { get; set; }
    public int TotalExams { get; set; }
    public List<TeacherDto> Teachers { get; set; } = new();
}

public class SubjectStudyDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnrolled { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int ProgressPercentage { get; set; }
    public List<TeacherDto> Teachers { get; set; } = new();
    public List<ChapterStudyDto> Chapters { get; set; } = new();
    public List<ExamSummaryDto> SubjectExams { get; set; } = new();
}

public class ChapterStudyDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long SubjectId { get; set; }
    public List<LessonStudyDto> Lessons { get; set; } = new();
    public List<ExamSummaryDto> ChapterExams { get; set; } = new();
    public int TotalLessons => Lessons.Count;
    public int CompletedLessons { get; set; }
    public int ProgressPercentage => TotalLessons > 0 ? (int)Math.Round((double)CompletedLessons / TotalLessons * 100) : 0;
}

public class LessonStudyDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long ChapterId { get; set; }
    public string ChapterName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class LessonDetailDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public long ChapterId { get; set; }
    public string ChapterName { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? PreviousLessonId { get; set; }
    public string? PreviousLessonTitle { get; set; }
    public long? NextLessonId { get; set; }
    public string? NextLessonTitle { get; set; }
}
