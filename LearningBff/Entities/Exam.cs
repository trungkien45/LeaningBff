using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LearningBff.Entities;

public class Exam : FullAuditedAggregateRoot<long>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    // Association with Subject,
    // indicating the subject to which this exam belongs.
    // useful for exams that are not tied to a specific chapter.
    public long SubjectId { get; set; }
    public Subject? Subject { get; set; }
    // Optional association with Chapter,
    // useful for exams that are specific to a chapter within a subject.
    public long? ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    public int DurationInMinutes { get; set; } = 45;
    public float PassScore { get; set; } = 5.0f;
    public float MaxScore { get; set; } = 10.0f;

    public bool IsPublished { get; set; }
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleAnswers { get; set; } = true;

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxAttempts { get; set; }

    public List<ExamQuestion> ExamQuestions { get; set; } = new();
    public List<ExamResult> ExamResults { get; set; } = new();

    public Exam()
    {
    }

    public Exam(string title, long subjectId, int durationInMinutes = 45, float passScore = 5.0f, float maxScore = 10.0f)
    {
        Title = title;
        SubjectId = subjectId;
        DurationInMinutes = durationInMinutes;
        PassScore = passScore;
        MaxScore = maxScore;
    }
}
