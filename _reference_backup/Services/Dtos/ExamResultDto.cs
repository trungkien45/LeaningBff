using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LearningBff.Entities;

namespace LearningBff.Services.Dtos;

public class ExamResultDto
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public float Score { get; set; }
    public float MaxScore { get; set; }
    public bool IsPassed { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? SubmitTime { get; set; }
    public ExamResultStatus Status { get; set; }
    public int AttemptNumber { get; set; }
    public TimeSpan? Duration => SubmitTime.HasValue ? SubmitTime.Value - StartTime : null;

    /// <summary>Danh sách đáp án student đã chọn (1 dòng per answer chọn)</summary>
    public List<ExamResultAnswerDto> Answers { get; set; } = new();

    /// <summary>Danh sách câu hỏi với đầy đủ options để review</summary>
    public List<ExamResultQuestionDto> Questions { get; set; } = new();
}

public class ExamResultAnswerDto
{
    public long QuestionId { get; set; }
    public string QuestionTitle { get; set; } = string.Empty;
    public long? AnswerId { get; set; }
    public string? AnswerText { get; set; }
    public bool IsCorrect { get; set; }
    public float Score { get; set; }
    public string? Explanation { get; set; }
}

/// <summary>Một câu hỏi đầy đủ để review: tất cả options, đáp án đúng, đáp án student chọn</summary>
public class ExamResultQuestionDto
{
    public long QuestionId { get; set; }
    public string QuestionTitle { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType QuestionType { get; set; }
    public bool IsCorrect { get; set; }
    public float EarnedScore { get; set; }
    public string? GeneralExplanation { get; set; }

    /// <summary>Tất cả đáp án của câu hỏi</summary>
    public List<ExamResultOptionDto> Options { get; set; } = new();
}

public class ExamResultOptionDto
{
    public long AnswerId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrectAnswer { get; set; }   // đáp án đúng theo đề
    public bool IsSelectedByStudent { get; set; } // student có chọn không
    public string? Explanation { get; set; }
}

/// <summary>Student nộp bài</summary>
public class SubmitExamDto
{
    public long ExamResultId { get; set; }
    public List<StudentAnswerDto> Answers { get; set; } = new();
}

public class StudentAnswerDto
{
    public long QuestionId { get; set; }
    /// <summary>Danh sách answer IDs học sinh chọn (multi-choice có thể chọn nhiều)</summary>
    public List<long> SelectedAnswerIds { get; set; } = new();
}
