using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Identity;

namespace LearningBff.Entities
{
    public class LearningProgress : FullAuditedAggregateRoot<long>
    {

        [NotMapped]
        public Guid StudentId
        {
            get => UserId;
            set => UserId = value;
        }
        public Guid UserId { get; set; }
        public IdentityUser User { get; set; } = null!;
        public long LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }

    }
}
