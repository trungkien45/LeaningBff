namespace LearningBff.Services.Dtos;

public class TeacherDashboardDto
{
    public int TotalSubjects { get; set; }
    public int TotalStudents { get; set; }
    public int TotalExams { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalSubmittedExams { get; set; }
    public int TotalPassedExams { get; set; }
    public float PassRate => TotalSubmittedExams > 0
        ? (float)Math.Round((double)TotalPassedExams / TotalSubmittedExams * 100, 1)
        : 0;
    public List<TeacherSubjectStatDto> SubjectStats { get; set; } = new();
    public List<RecentSubmissionDto> RecentSubmissions { get; set; } = new();

    // Pagination & Search for SubjectStats
    public string? SubjectSearch { get; set; }
    public int SubjectCurrentPage { get; set; } = 1;
    public int SubjectPageSize { get; set; } = 6;
    public int SubjectTotalCount { get; set; }
    public int SubjectTotalPages => SubjectPageSize > 0
        ? (int)Math.Ceiling((double)SubjectTotalCount / SubjectPageSize)
        : 0;
    public int SubjectStartIndex => SubjectTotalCount == 0 ? 0 : (SubjectCurrentPage - 1) * SubjectPageSize + 1;
    public int SubjectEndIndex => Math.Min(SubjectCurrentPage * SubjectPageSize, SubjectTotalCount);

    public int SubjectStartPage
    {
        get
        {
            const int maxDisplay = 5;
            int start = Math.Max(1, SubjectCurrentPage - maxDisplay / 2);
            int end = Math.Min(SubjectTotalPages, start + maxDisplay - 1);
            if (end - start + 1 < maxDisplay)
            {
                start = Math.Max(1, end - maxDisplay + 1);
            }
            return start;
        }
    }

    public int SubjectEndPage
    {
        get
        {
            const int maxDisplay = 5;
            int start = Math.Max(1, SubjectCurrentPage - maxDisplay / 2);
            int end = Math.Min(SubjectTotalPages, start + maxDisplay - 1);
            if (end - start + 1 < maxDisplay)
            {
                end = Math.Min(SubjectTotalPages, start + maxDisplay - 1);
            }
            return end;
        }
    }
}
