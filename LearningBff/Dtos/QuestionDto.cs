using LearningBff.Entities;

namespace LearningBff.Dtos;

public class QuestionDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public QuestionDifficulty Difficulty { get; set; }
    public QuestionType Type { get; set; }
    public string? GeneralExplanation { get; set; }
    public float DefaultScore { get; set; }
    public int Order { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}

