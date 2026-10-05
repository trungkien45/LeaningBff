namespace LearningBff.Services.Dtos;

public class CreateUpdateAnswerDto
{
    public string? Text { get; internal set; }
    public bool IsCorrect { get; internal set; }
    public int Order { get; internal set; }
}
public class CreateAnswerDto : CreateUpdateAnswerDto
{
}
public class UpdateAnswerDto : CreateUpdateAnswerDto
{
}