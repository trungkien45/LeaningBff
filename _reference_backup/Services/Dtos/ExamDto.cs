using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LearningBff.Entities;

namespace LearningBff.Services.Dtos;

public class ExamSummaryDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int DurationInMinutes { get; set; }
    public float PassScore { get; set; }
    public float MaxScore { get; set; }
    public bool IsPublished { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxAttempts { get; set; }
    public int QuestionCount { get; set; }
    public List<ExamQuestionDto> ExamQuestions { get; set; } = new();
}

public class ExamDto : ExamSummaryDto
{
}

public class ExamQuestionDto
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public string QuestionTitle { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType QuestionType { get; set; }
    public float Score { get; set; }
    public int Order { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}

public class CreateExamDto
{
    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string? Description { get; set; }

    [Required]
    public long SubjectId { get; set; }

    public int DurationInMinutes { get; set; } = 45;
    public float PassScore { get; set; } = 5.0f;
    public float MaxScore { get; set; } = 10.0f;

    public bool IsPublished { get; set; }
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleAnswers { get; set; } = true;

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxAttempts { get; set; }
    public List<AddExamQuestionDto> Questions { get; set; } = new();
}

public class UpdateExamDto : CreateExamDto
{
}

public class AddExamQuestionDto
{
    public long QuestionId { get; set; }
    public float Score { get; set; } = 1.0f;
    public int Order { get; set; }
}
