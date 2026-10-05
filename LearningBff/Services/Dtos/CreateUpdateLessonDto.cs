namespace LearningBff.Services.Dtos;

public class CreateUpdateLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
}
public class CreateLessonDto : CreateUpdateLessonDto
{
}
public class UpdateLessonDto : CreateUpdateLessonDto
{
}