namespace LearningBff.Services.Dtos;

public class RecentSubmissionDto
{
    public long ResultId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public float Score { get; set; }
    public float MaxScore { get; set; }
    public bool IsPassed { get; set; }
    public DateTime SubmitTime { get; set; }
}
