namespace LearningBff.Services.Dtos;

public class CreateUpdateChapterDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
public class UpdateChapterDto : CreateUpdateChapterDto
{
}
public class CreateChapterDto : CreateUpdateChapterDto
{
}