using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LearningBff.Entities;

public class Question : FullAuditedEntity<long>
{
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Multi or Single choice question type. Multi choice question type can have multiple correct answers, while single choice question type can have only one correct answer.
    /// </summary>
    public QuestionType Type { get; set; } = QuestionType.Single;

    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;
    public string? GeneralExplanation { get; set; }
    public float DefaultScore { get; set; } = 1.0f;
    public int Order { get; set; }

    public List<Answer> Answers { get; set; } = [];
    public Subject? Subject { get; set; }
    public List<ExamQuestion> ExamQuestions { get; set; } = [];
    public List<ExamResultAnswer> ExamResultAnswers { get; set; } = [];
    public long SubjectId { get; set; }

    public Question()
    {
    }

    public Question(string title, QuestionType type, long subjectId, QuestionDifficulty difficulty = QuestionDifficulty.Medium, float defaultScore = 1.0f)
    {
        Title = title;
        Type = type;
        SubjectId = subjectId;
        Difficulty = difficulty;
        DefaultScore = defaultScore;
    }
}
