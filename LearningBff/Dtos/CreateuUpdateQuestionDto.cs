using LearningBff.Entities;

namespace LearningBff.Dtos
{
    public class CreateuUpdateQuestionDto
    {
        public string Title { get; set; } = string.Empty;
        public QuestionDifficulty Difficulty { get; set; }
        public QuestionType Type { get; set; }
        public string GeneralExplanation { get; set; } = string.Empty;
        public float DefaultScore { get; set; }
        public int Order { get; set; }
    }
    public class CreateQuestionDto : CreateuUpdateQuestionDto
    {
    }
    public class UpdateQuestionDto : CreateuUpdateQuestionDto
    {
    }
}
