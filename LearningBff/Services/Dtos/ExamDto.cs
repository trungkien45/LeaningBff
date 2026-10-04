using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LearningBff.Entities;

namespace LearningBff.Services.Dtos;

public class SubjectFilterDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ExamSummaryDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public long? ChapterId { get; set; }
    public string? ChapterName { get; set; }
    public int DurationInMinutes { get; set; }
    public float PassScore { get; set; }
    public float MaxScore { get; set; }
    public bool IsPublished { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }
    public bool CanTake { get; set; } = true;
    public int QuestionCount { get; set; }
}

public class ExamTakeDto
{
    public long ExamResultId { get; set; }
    public long ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int DurationInMinutes { get; set; }
    public int RemainingSeconds { get; set; }
    public DateTime StartTime { get; set; }
    public List<ExamTakeQuestionDto> Questions { get; set; } = new();
}

public class ExamTakeQuestionDto
{
    public long QuestionId { get; set; }
    public string Title { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType Type { get; set; }
    public float Score { get; set; }
    public int Order { get; set; }
    public List<AnswerItemDto> Answers { get; set; } = new();
}

public class AnswerItemDto
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class SubmitExamDto
{
    public long ExamResultId { get; set; }
    public List<StudentAnswerDto> Answers { get; set; } = new();
}

public class StudentAnswerDto
{
    public long QuestionId { get; set; }
    public List<long> SelectedAnswerIds { get; set; } = new();
}

public class ExamResultDto
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public float Score { get; set; }
    public float MaxScore { get; set; }
    public bool IsPassed { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? SubmitTime { get; set; }
    public ExamResultStatus Status { get; set; }
    public int AttemptNumber { get; set; }
    public bool CanRetake { get; set; }
    public TimeSpan? Duration => SubmitTime.HasValue ? SubmitTime.Value - StartTime : null;
    public int TotalQuestions { get; set; }
    public int CorrectQuestions { get; set; }
    public List<ExamResultQuestionDto> Questions { get; set; } = new();
}

public class ExamResultQuestionDto
{
    public long QuestionId { get; set; }
    public string QuestionTitle { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType QuestionType { get; set; }
    public bool IsCorrect { get; set; }
    public float EarnedScore { get; set; }
    public float MaxScore { get; set; }
    public string? GeneralExplanation { get; set; }
    public List<ExamResultOptionDto> Options { get; set; } = new();
}

public class ExamResultOptionDto
{
    public long AnswerId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrectAnswer { get; set; }
    public bool IsSelectedByStudent { get; set; }
    public string? Explanation { get; set; }
}
