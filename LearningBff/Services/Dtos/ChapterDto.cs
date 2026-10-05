namespace LearningBff.Services.Dtos;

public class ChapterDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long SubjectId { get; set; }
    public DateTime CreationTime { get; set; }
}