using System.ComponentModel.DataAnnotations;

namespace LearningBff.Dtos;

public class ImportQuestionDto
{
    [Required]
    public long SubjectId { get; set; }
    [Required]
    public IFormFile File { get; set; } = null!;
}

