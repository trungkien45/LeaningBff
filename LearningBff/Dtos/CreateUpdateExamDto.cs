namespace LearningBff.Dtos;

public class CreateUpdateExamDto
{
    public string? Title { get; set; }
    public int DurationInMinutes { get; set; }
    public float PassScore { get; set; }
    public float MaxScore { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxAttempts { get; set; }
}
public class CreateExamDto : CreateUpdateExamDto
{
}
public class UpdateExamDto : CreateUpdateExamDto
{
}