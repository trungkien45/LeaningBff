namespace LearningBff.Dtos;

public class CreateUpdateAnswerDto
{
    public string? Text { get; set; }
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}
public class CreateAnswerDto : CreateUpdateAnswerDto
{
    public long Id { get; set; }
}
public class UpdateAnswerDto : CreateUpdateAnswerDto
{
}