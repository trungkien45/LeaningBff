using LearningBff.Entities;

namespace LearningBff.Services.Dtos;

public class QuestionDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public QuestionDifficulty Difficulty { get; set; }
    public QuestionType Type { get; set; }
    public string? GeneralExplanation { get; set; }
    public float DefaultScore { get; set; }
    public int Order { get; set; }
}
