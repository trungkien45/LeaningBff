namespace LearningBff.Dtos;

public class LessonDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long ChapterId { get; set; }
    public DateTime CreationTime { get; set; }
    public string? Content { get; set; }
}
