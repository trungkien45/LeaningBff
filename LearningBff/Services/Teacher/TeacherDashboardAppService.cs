using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LearningBff.Entities;
using LearningBff.Dtos;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LearningBff.Services.Teacher;

public class TeacherDashboardAppService : TeacherBaseAppService  
{
    private record ExamStatRow(long SubjectId, int SubmissionCount, int PassedCount, float AvgScore);

    private readonly IRepository<EnrollmentSubject, long> _enrollmentSubjectRepository;
    private readonly IRepository<Exam, long> _examRepository;
    private readonly IRepository<Question, long> _questionRepository;
    private readonly IRepository<ExamResult, long> _examResultRepository;

    public TeacherDashboardAppService(
        IRepository<Subject, long> subjectRepository,
        IRepository<EnrollmentSubject, long> enrollmentSubjectRepository,
        IRepository<Exam, long> examRepository,
        IRepository<Question, long> questionRepository,
        IRepository<ExamResult, long> examResultRepository,
        ICurrentUser currentUser) : base(subjectRepository, currentUser)
    {
        _enrollmentSubjectRepository = enrollmentSubjectRepository;
        _examRepository = examRepository;
        _questionRepository = questionRepository;
        _examResultRepository = examResultRepository;
    }

    /// <summary>
    /// Lấy dữ liệu Dashboard tổng quan cho Giáo viên kèm phân trang và tìm kiếm môn học
    /// </summary>
    public async Task<TeacherDashboardDto> GetDashboardAsync(
        string? search = null,
        int page = 1,
        int pageSize = 6)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Vui lòng đăng nhập.");

        // Các môn học mà giáo viên này phụ trách (giữ dạng sub-query, không nạp danh sách ID vào RAM)
        var mySubjectIdsQuery = (await _subjectRepository.GetQueryableAsync())
            .Where(s => s.Teachers.Any(t => t.Id == userId))
            .Select(s => s.Id);

        var baseSubjectQuery = (await _subjectRepository.GetQueryableAsync())
            .Where(s => mySubjectIdsQuery.Contains(s.Id));

        // 1. Thống kê tổng quan cho Stat Cards (Query trực tiếp trên DB, không kéo dữ liệu vào RAM)
        var totalSubjects = await baseSubjectQuery.CountAsync();

        var totalStudents = await (await _enrollmentSubjectRepository.GetQueryableAsync())
            .Where(e => mySubjectIdsQuery.Contains(e.SubjectId) && e.IsActive)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync();

        var totalExams = await (await _examRepository.GetQueryableAsync())
            .CountAsync(e => mySubjectIdsQuery.Contains(e.SubjectId));

        var totalQuestions = await (await _questionRepository.GetQueryableAsync())
            .CountAsync(q => mySubjectIdsQuery.Contains(q.SubjectId));

        var totalSubmittedExams = await (await _examResultRepository.GetQueryableAsync())
            .CountAsync(r => r.Exam != null && mySubjectIdsQuery.Contains(r.Exam.SubjectId) && r.Status == ExamResultStatus.Submitted);

        var totalPassedExams = await (await _examResultRepository.GetQueryableAsync())
            .CountAsync(r => r.Exam != null && mySubjectIdsQuery.Contains(r.Exam.SubjectId) && r.Status == ExamResultStatus.Submitted && r.IsPassed);

        // 2. Lấy 10 bài nộp gần nhất (Chỉ load đúng 10 bản ghi từ SQL)
        var recent = await (await _examResultRepository.GetQueryableAsync())
            .Where(r => r.Exam != null && mySubjectIdsQuery.Contains(r.Exam.SubjectId) && r.Status == ExamResultStatus.Submitted)
            .OrderByDescending(r => r.SubmitTime)
            .Take(10)
            .Select(r => new RecentSubmissionDto
            {
                ResultId = r.Id,
                StudentName = r.User != null ? (!string.IsNullOrEmpty(r.User.Name) ? r.User.Name : r.User.UserName) : "Học viên",
                ExamTitle = r.ExamTitle ?? (r.Exam != null ? r.Exam.Title : "—"),
                SubjectName = r.Exam != null && r.Exam.Subject != null ? r.Exam.Subject.Name : "—",
                Score = r.Score,
                MaxScore = r.MaxScore,
                IsPassed = r.IsPassed,
                SubmitTime = r.SubmitTime ?? r.CreationTime
            })
            .ToListAsync();

        // 3. Phân trang và tìm kiếm danh sách môn học của giáo viên
        var subjectQuery = baseSubjectQuery;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            subjectQuery = subjectQuery.Where(s => s.Name.ToLower().Contains(term));
        }

        var totalFilteredSubjects = await subjectQuery.CountAsync();
        var safePage = Math.Max(1, page);
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalFilteredSubjects / pageSize) : 1;
        if (totalPages > 0 && safePage > totalPages)
        {
            safePage = totalPages;
        }

        var pagedSubjects = await subjectQuery
            .Include(s => s.Exams)
            .Include(s => s.EnrollmentSubjects)
            .OrderBy(s => s.Name)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pageSubjectIds = pagedSubjects.Select(s => s.Id).ToList();

        // 4. Thống kê kết quả thi cho các môn trên trang hiện tại trực tiếp bằng SQL GroupBy
        var examStats = pageSubjectIds.Any()
            ? await (await _examResultRepository.GetQueryableAsync())
                .Where(r => r.Exam != null && pageSubjectIds.Contains(r.Exam.SubjectId) && r.Status == ExamResultStatus.Submitted)
                .GroupBy(r => r.Exam!.SubjectId)
                .Select(g => new ExamStatRow(
                    g.Key,
                    g.Count(),
                    g.Count(r => r.IsPassed),
                    (float)g.Average(r => (double)r.Score)
                ))
                .ToListAsync()
            : new List<ExamStatRow>();

        var examStatDict = examStats.ToDictionary(x => x.SubjectId);

        var subjectStats = pagedSubjects.Select(s =>
        {
            examStatDict.TryGetValue(s.Id, out var stat);
            var studentCount = s.EnrollmentSubjects.Count(e => e.IsActive);

            return new TeacherSubjectStatDto
            {
                SubjectId = s.Id,
                SubjectName = s.Name,
                StudentCount = studentCount,
                ExamCount = s.Exams.Count,
                SubmissionCount = stat?.SubmissionCount ?? 0,
                PassedCount = stat?.PassedCount ?? 0,
                AvgScore = stat != null ? (float)Math.Round(stat.AvgScore, 2) : 0
            };
        }).ToList();

        return new TeacherDashboardDto
        {
            TotalSubjects = totalSubjects,
            TotalStudents = totalStudents,
            TotalExams = totalExams,
            TotalQuestions = totalQuestions,
            TotalSubmittedExams = totalSubmittedExams,
            TotalPassedExams = totalPassedExams,
            SubjectStats = subjectStats,
            RecentSubmissions = recent,
            SubjectSearch = search,
            SubjectCurrentPage = safePage,
            SubjectPageSize = pageSize,
            SubjectTotalCount = totalFilteredSubjects
        };
    }
}