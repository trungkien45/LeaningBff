using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LearningBff.Entities;

namespace LearningBff.Services.Dtos;

public class AnswerDto
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
    public int Order { get; set; }
}

public class CreateAnswerDto
{
    public long Id { get; set; } = 0;
    [Required]
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
    public int Order { get; set; }
}

public class QuestionDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType Type { get; set; }
    public QuestionDifficulty Difficulty { get; set; }
    public string? GeneralExplanation { get; set; }
    public float DefaultScore { get; set; }
    public int Order { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public List<AnswerDto> Answers { get; set; } = new();
}
public class ImportQuestionDto
{
    [Required]
    public long SubjectId { get; set; }
    [Required]
    public IFormFile File { get; set; } = null!;
}
public class ImportQuestionResultDto
{
    public int TotalQuestions { get; set; }
    public int ImportedQuestions { get; set; }
    public List<string> Errors { get; set; } = new();
}
public class CreateQuestionDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType Type { get; set; } = QuestionType.Single;

    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;
    public string? GeneralExplanation { get; set; }
    public float DefaultScore { get; set; } = 1.0f;

    [Required]
    public long SubjectId { get; set; }

    public List<CreateAnswerDto> Answers { get; set; } = new();
}

public class UpdateQuestionDto : CreateQuestionDto
{
}
