using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Identity;

namespace LearningBff.Entities
{
    public class EnrollmentSubject : FullAuditedAggregateRoot<long>
    {
        public Guid UserId { get; set; }
        public IdentityUser? User { get; set; }

        [NotMapped]
        public Guid StudentId
        {
            get => UserId;
            set => UserId = value;
        }

        public long SubjectId { get; set; }
        public Subject? Subject { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? UnregisteredAt { get; set; }
    }
}