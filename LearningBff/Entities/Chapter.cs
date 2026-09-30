using Volo.Abp.Domain.Entities.Auditing;

namespace LearningBff.Entities
{
    public class Chapter: FullAuditedAggregateRoot<long>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;
        public List<Exam> Exams { get; set; } = [];
        public List<Lesson> Lessons { get; set; } = [];
        public Chapter()
        {
        }
        public Chapter(string name, long subjectId, string? description = null)
        {
            Name = name;
            SubjectId = subjectId;
            Description = description;
        }
    }
}
