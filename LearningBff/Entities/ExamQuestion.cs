using Volo.Abp.Domain.Entities;

namespace LearningBff.Entities;

public class ExamQuestion : Entity<long>
{
    public long ExamId { get; set; }
    public Exam? Exam { get; set; }

    public long QuestionId { get; set; }
    public Question? Question { get; set; }

    public float Score { get; set; } = 1.0f;
    public int Order { get; set; }

    public ExamQuestion()
    {
    }

    public ExamQuestion(long examId, long questionId, float score = 1.0f, int order = 0)
    {
        ExamId = examId;
        QuestionId = questionId;
        Score = score;
        Order = order;
    }
}
