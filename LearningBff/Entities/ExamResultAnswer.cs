using Volo.Abp.Domain.Entities;

namespace LearningBff.Entities;

public class ExamResultAnswer : Entity<long>
{
    public long ExamResultId { get; set; }
    public ExamResult? ExamResult { get; set; }

    public long QuestionId { get; set; }
    public Question? Question { get; set; }

    public long AnswerId { get; set; }
    public Answer? Answer { get; set; }

    public bool IsSelected { get; set; } = true;
    public bool IsCorrect { get; set; }
    public float EarnedScore { get; set; }

    public ExamResultAnswer()
    {
    }

    public ExamResultAnswer(long examResultId, long questionId, long answerId, bool isCorrect = false, float earnedScore = 0.0f)
    {
        ExamResultId = examResultId;
        QuestionId = questionId;
        AnswerId = answerId;
        IsSelected = true;
        IsCorrect = isCorrect;
        EarnedScore = earnedScore;
    }
}
