using Volo.Abp.Domain.Entities.Auditing;

namespace LearningBff.Entities;

public class Answer : FullAuditedEntity<long>
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
    public int Order { get; set; }

    public Question? Question { get; set; }
    public long QuestionId { get; set; }
    public List<ExamResultAnswer> ExamResultAnswers { get; set; } = [];
    public Answer()
    {
    }

    public Answer(string text, bool isCorrect, long questionId, int order = 0, string? explanation = null)
    {
        Text = text;
        IsCorrect = isCorrect;
        QuestionId = questionId;
        Order = order;
        Explanation = explanation;
    }
}
