namespace LearningBff.Dtos;

public class ImportQuestionResultDto
{
    public int TotalQuestions { get; set; }
    public int ImportedQuestions { get; set; }
    public List<string> Errors { get; set; } = new();
}

