using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Volo.Abp;
using Volo.Abp.Users;

namespace LearningBff.Services;

public class TeacherDashboardDto
{
    public int TotalSubjects { get; set; }
    public int TotalStudents { get; set; }
    public int TotalExams { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalSubmittedExams { get; set; }
    public int TotalPassedExams { get; set; }
    public float PassRate => TotalSubmittedExams > 0
        ? (float)Math.Round((double)TotalPassedExams / TotalSubmittedExams * 100, 1)
        : 0;
    public List<TeacherSubjectStatDto> SubjectStats { get; set; } = new();
    public List<RecentSubmissionDto> RecentSubmissions { get; set; } = new();
}

public class TeacherSubjectStatDto
{
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int ExamCount { get; set; }
    public int SubmissionCount { get; set; }
    public int PassedCount { get; set; }
    public float PassRate => SubmissionCount > 0
        ? (float)Math.Round((double)PassedCount / SubmissionCount * 100, 1)
        : 0;
    public float AvgScore { get; set; }
}

public class RecentSubmissionDto
{
    public long ResultId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public float Score { get; set; }
    public float MaxScore { get; set; }
    public bool IsPassed { get; set; }
    public DateTime SubmitTime { get; set; }
}

public class TeacherAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;
    private readonly ICurrentUser _currentUser;

    public TeacherAppService(LearningBffDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Lấy dữ liệu Dashboard tổng quan cho Giáo viên
    /// </summary>
    public async Task<TeacherDashboardDto> GetDashboardAsync()
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Vui lòng đăng nhập.");

        // Các môn học mà giáo viên này phụ trách
        var mySubjectIds = await _db.Subjects.AsNoTracking()
            .Where(s => s.Teachers.Any(t => t.Id == userId))
            .Select(s => s.Id)
            .ToListAsync();

        // Nếu là admin: xem tất cả môn học
        var isAdminOrNoSubject = !mySubjectIds.Any();
        var subjectQuery = isAdminOrNoSubject
            ? _db.Subjects.AsNoTracking()
            : _db.Subjects.AsNoTracking().Where(s => mySubjectIds.Contains(s.Id));

        var subjects = await subjectQuery
            .Include(s => s.Exams)
            .Include(s => s.EnrollmentSubjects)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var allSubjectIds = subjects.Select(s => s.Id).ToList();

        // Tổng số sinh viên (distinct) đang đăng ký ít nhất một môn giảng viên dạy
        var totalStudents = await _db.EnrollmentSubjects.AsNoTracking()
            .Where(e => allSubjectIds.Contains(e.SubjectId) && e.IsActive)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync();

        // Thống kê kết quả thi
        var examIds = subjects.SelectMany(s => s.Exams.Select(e => e.Id)).ToList();

        var submittedResults = await _db.ExamResults.AsNoTracking()
            .Where(r => examIds.Contains(r.ExamId) && r.Status == ExamResultStatus.Submitted)
            .Include(r => r.User)
            .Include(r => r.Exam)
                .ThenInclude(e => e!.Subject)
            .OrderByDescending(r => r.SubmitTime)
            .ToListAsync();

        // Per-subject stats
        var subjectStats = subjects.Select(s =>
        {
            var sExamIds = s.Exams.Select(e => e.Id).ToList();
            var sResults = submittedResults.Where(r => sExamIds.Contains(r.ExamId)).ToList();
            var studentCount = _db.EnrollmentSubjects
                .Count(e => e.SubjectId == s.Id && e.IsActive);

            return new TeacherSubjectStatDto
            {
                SubjectId = s.Id,
                SubjectName = s.Name,
                StudentCount = studentCount,
                ExamCount = s.Exams.Count,
                SubmissionCount = sResults.Count,
                PassedCount = sResults.Count(r => r.IsPassed),
                AvgScore = sResults.Any()
                    ? (float)Math.Round(sResults.Average(r => r.Score), 2)
                    : 0
            };
        }).ToList();

        // 10 bài nộp gần nhất
        var recent = submittedResults.Take(10).Select(r => new RecentSubmissionDto
        {
            ResultId = r.Id,
            StudentName = r.User?.Name ?? r.User?.UserName ?? "Học viên",
            ExamTitle = r.ExamTitle ?? r.Exam?.Title ?? "—",
            SubjectName = r.Exam?.Subject?.Name ?? "—",
            Score = r.Score,
            MaxScore = r.MaxScore,
            IsPassed = r.IsPassed,
            SubmitTime = r.SubmitTime ?? r.CreationTime
        }).ToList();

        return new TeacherDashboardDto
        {
            TotalSubjects = subjects.Count,
            TotalStudents = totalStudents,
            TotalExams = subjects.Sum(s => s.Exams.Count),
            TotalQuestions = await _db.Questions.AsNoTracking()
                .CountAsync(q => allSubjectIds.Contains(q.SubjectId)),
            TotalSubmittedExams = submittedResults.Count,
            TotalPassedExams = submittedResults.Count(r => r.IsPassed),
            SubjectStats = subjectStats,
            RecentSubmissions = recent
        };
    }
}
