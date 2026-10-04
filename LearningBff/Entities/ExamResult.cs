using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Identity;

namespace LearningBff.Entities;

public class ExamResult : FullAuditedAggregateRoot<long>
{
    public long ExamId { get; set; }
    public string? ExamTitle { get; set; }
    public Exam? Exam { get; set; }

    public List<ExamResultAnswer> ExamResultAnswers { get; set; } = [];

    /// <summary>
    /// Student (User) ID in ABP Identity (AbpUsers.Id)
    /// </summary>
    public Guid UserId { get; set; }
    public IdentityUser? User { get; set; }

    /// <summary>
    /// Alias helper for StudentId (links to UserId)
    /// </summary>
    [NotMapped]
    public Guid StudentId
    {
        get => UserId;
        set => UserId = value;
    }

    public float Score { get; set; }
    public float MaxScore { get; set; } = 10.0f;
    public bool IsPassed { get; set; }

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? SubmitTime { get; set; }

    public ExamResultStatus Status { get; set; } = ExamResultStatus.InProgress;
    public int AttemptNumber { get; set; } = 1;

    public ExamResult()
    {
    }

    public ExamResult(long examId, Guid userId, int attemptNumber = 1)
    {
        ExamId = examId;
        UserId = userId;
        AttemptNumber = attemptNumber;
        StartTime = DateTime.UtcNow;
        Status = ExamResultStatus.InProgress;
    }
}
