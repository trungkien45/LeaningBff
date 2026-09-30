using Volo.Abp.Domain.Entities.Auditing;

namespace LearningBff.Entities
{
    public class Lesson : FullAuditedAggregateRoot<long>
    {
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public long ChapterId { get; set; }
        public Chapter Chapter { get; set; } = null!;
        public List<LearningProgress> LearningProgresses { get; set; } = [];
    }
}
