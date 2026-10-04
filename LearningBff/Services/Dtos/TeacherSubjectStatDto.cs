namespace LearningBff.Services.Dtos;

public class TeacherSubjectStatDto
{
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int ExamCount { get; set; }
    public int SubmissionCount { get; set; }
    public int PassedCount { get; set; }
    public float PassRate => SubmissionCount > 0
        ? (float)Math.Round((double)PassedCount / SubmissionCount * 100, 1)
        : 0;
    public float AvgScore { get; set; }
}
