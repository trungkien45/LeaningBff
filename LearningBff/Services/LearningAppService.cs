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

namespace LearningBff.Services;

public class LearningAppService : LearningBffAppService
{
    private record LessonStatRow(long SubjectId, int TotalLessons, int CompletedLessons);

    private readonly IRepository<EnrollmentSubject, long> _enrollmentSubjectRepository;
    private readonly IRepository<Subject, long> _subjectRepository;
    private readonly IRepository<LearningProgress, long> _learningProgressRepository;
    private readonly IRepository<Lesson, long> _lessonRepository;
    private readonly IRepository<Chapter, long> _chapterRepository;
    private readonly ICurrentUser _currentUser;

    public LearningAppService(
        IRepository<EnrollmentSubject, long> enrollmentSubjectRepository,
        IRepository<Subject, long> subjectRepository,
        IRepository<LearningProgress, long> learningProgressRepository,
        IRepository<Lesson, long> lessonRepository,
        IRepository<Chapter, long> chapterRepository,
        ICurrentUser currentUser)
    {
        _enrollmentSubjectRepository = enrollmentSubjectRepository;
        _subjectRepository = subjectRepository;
        _learningProgressRepository = learningProgressRepository;
        _lessonRepository = lessonRepository;
        _chapterRepository = chapterRepository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Danh sách các môn học mà học viên hiện tại đã đăng ký
    /// </summary>
    public async Task<(List<SubjectCardDto> Items, int TotalCount)> GetMyCoursesAsync(
        int page = 1,
        int pageSize = 9)
    {
        var userId = _currentUser.Id;
        if (!userId.HasValue)
        {
            return (new List<SubjectCardDto>(), 0);
        }

        var enrollQuery = (await _enrollmentSubjectRepository.GetQueryableAsync())
            .Where(e => e.UserId == userId.Value && e.IsActive);

        var totalCount = await enrollQuery.CountAsync();

        var safePage = Math.Max(1, page);
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
        if (totalPages > 0 && safePage > totalPages) safePage = totalPages;

        var enrollments = await enrollQuery
            .Include(e => e.Subject)
                .ThenInclude(s => s!.Teachers)
            .Include(e => e.Subject)
                .ThenInclude(s => s!.Exams)
            .OrderByDescending(e => e.RegisteredAt)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Số chương / bài học / bài đã hoàn thành của các môn trong trang hiện tại (gom nhóm ngay trong SQL)
        var pageSubjectIds = enrollments.Select(e => e.SubjectId).Distinct().ToList();

        var lessonStatDict = await (await _lessonRepository.GetQueryableAsync())
            .Where(l => pageSubjectIds.Contains(l.Chapter.SubjectId))
            .GroupBy(l => l.Chapter.SubjectId)
            .Select(g => new LessonStatRow(
                g.Key,
                g.Count(),
                g.Count(l => l.LearningProgresses.Any(p => p.UserId == userId.Value && p.IsCompleted))))
            .ToDictionaryAsync(x => x.SubjectId);

        var chapterCountDict = await (await _chapterRepository.GetQueryableAsync())
            .Where(c => pageSubjectIds.Contains(c.SubjectId))
            .GroupBy(c => c.SubjectId)
            .Select(g => new { SubjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SubjectId, x => x.Count);

        var result = new List<SubjectCardDto>();
        foreach (var enrollment in enrollments)
        {
            var s = enrollment.Subject;
            if (s == null) continue;

            lessonStatDict.TryGetValue(s.Id, out var lessonStat);
            chapterCountDict.TryGetValue(s.Id, out var chapterCount);
            var totalLessons = lessonStat?.TotalLessons ?? 0;
            var completedCount = lessonStat?.CompletedLessons ?? 0;
            var progress = totalLessons > 0 ? (int)Math.Round((double)completedCount / totalLessons * 100) : 0;

            result.Add(new SubjectCardDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                IsEnrolled = true,
                EnrolledAt = enrollment.RegisteredAt,
                TotalChapters = chapterCount,
                TotalLessons = totalLessons,
                CompletedLessons = completedCount,
                ProgressPercentage = progress,
                TotalExams = s.Exams.Count(e => e.IsPublished),
                Teachers = s.Teachers.Select(t => new TeacherDto
                {
                    Id = t.Id,
                    UserName = t.UserName,
                    Name = t.Name,
                    Email = t.Email
                }).ToList()
            });
        }

        return (result, totalCount);
    }

    /// <summary>
    /// Tất cả môn học cho học viên khám phá và đăng ký
    /// </summary>
    public async Task<(List<SubjectCardDto> Items, int TotalCount)> GetAllCoursesAsync(
        string? search = null,
        int page = 1,
        int pageSize = 9)
    {
        var userId = _currentUser.Id;

        var query = (await _subjectRepository.GetQueryableAsync())
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term)
                || (s.Description != null && s.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var safePage = Math.Max(1, page);
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
        if (totalPages > 0 && safePage > totalPages) safePage = totalPages;

        var subjects = await query
            .Include(s => s.Teachers)
            .Include(s => s.Exams)
            .OrderBy(s => s.Name)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        HashSet<long> enrolledSubjectIds = new();
        Dictionary<long, LessonStatRow> lessonStatDict = new();
        Dictionary<long, int> chapterCountDict = new();

        if (userId.HasValue && subjects.Any())
        {
            var pageSubjectIds = subjects.Select(s => s.Id).ToList();

            enrolledSubjectIds = (await (await _enrollmentSubjectRepository.GetQueryableAsync())
                .Where(e => e.UserId == userId.Value && e.IsActive && pageSubjectIds.Contains(e.SubjectId))
                .Select(e => e.SubjectId)
                .ToListAsync()).ToHashSet();

            lessonStatDict = await (await _lessonRepository.GetQueryableAsync())
                .Where(l => pageSubjectIds.Contains(l.Chapter.SubjectId))
                .GroupBy(l => l.Chapter.SubjectId)
                .Select(g => new LessonStatRow(
                    g.Key,
                    g.Count(),
                    g.Count(l => l.LearningProgresses.Any(p => p.UserId == userId.Value && p.IsCompleted))))
                .ToDictionaryAsync(x => x.SubjectId);
        }

        if (subjects.Any())
        {
            var pageSubjectIds = subjects.Select(s => s.Id).ToList();

            chapterCountDict = await (await _chapterRepository.GetQueryableAsync())
                .Where(c => pageSubjectIds.Contains(c.SubjectId))
                .GroupBy(c => c.SubjectId)
                .Select(g => new { SubjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SubjectId, x => x.Count);
        }

        var result = new List<SubjectCardDto>();
        foreach (var s in subjects)
        {
            lessonStatDict.TryGetValue(s.Id, out var lessonStat);
            chapterCountDict.TryGetValue(s.Id, out var chapterCount);
            var totalLessons = lessonStat?.TotalLessons ?? 0;
            var completedCount = lessonStat?.CompletedLessons ?? 0;
            var progress = totalLessons > 0 ? (int)Math.Round((double)completedCount / totalLessons * 100) : 0;

            result.Add(new SubjectCardDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                IsEnrolled = enrolledSubjectIds.Contains(s.Id),
                TotalChapters = chapterCount,
                TotalLessons = totalLessons,
                CompletedLessons = completedCount,
                ProgressPercentage = progress,
                TotalExams = s.Exams.Count(e => e.IsPublished),
                Teachers = s.Teachers.Select(t => new TeacherDto
                {
                    Id = t.Id,
                    UserName = t.UserName,
                    Name = t.Name,
                    Email = t.Email
                }).ToList()
            });
        }

        return (result, totalCount);
    }

    /// <summary>
    /// Lấy chi tiết cây học tập của một môn học (Chương, Bài học, Đề thi, Tiến độ)
    /// </summary>
    public async Task<SubjectStudyDto> GetCourseStudyAsync(long subjectId)
    {
        var userId = _currentUser.Id;

        var subject = await (await _subjectRepository.GetQueryableAsync())
            .Include(s => s.Teachers)
            .Include(s => s.Chapters)
                .ThenInclude(c => c.Lessons)
            .Include(s => s.Chapters)
                .ThenInclude(c => c.Exams.Where(e => e.IsPublished))
            .Include(s => s.Exams.Where(e => e.IsPublished && e.ChapterId == null))
            .FirstOrDefaultAsync(s => s.Id == subjectId)
            ?? throw new UserFriendlyException("Không tìm thấy môn học.");

        var isEnrolled = false;
        var completedLessonDict = new Dictionary<long, DateTime?>();

        if (userId.HasValue)
        {
            isEnrolled = await _enrollmentSubjectRepository.AnyAsync(e =>
                e.UserId == userId.Value && e.SubjectId == subjectId && e.IsActive);

            var subjectLessonIdsQuery = (await _lessonRepository.GetQueryableAsync())
                .Where(l => l.Chapter.SubjectId == subjectId)
                .Select(l => l.Id);

            var progresses = await (await _learningProgressRepository.GetQueryableAsync())
                .Where(lp => lp.UserId == userId.Value && lp.IsCompleted && subjectLessonIdsQuery.Contains(lp.LessonId))
                .Select(lp => new { lp.LessonId, lp.CompletedAt })
                .ToListAsync();

            completedLessonDict = progresses.ToDictionary(p => p.LessonId, p => p.CompletedAt);
        }

        var allLessons = subject.Chapters.SelectMany(c => c.Lessons).ToList();
        var totalLessons = allLessons.Count;
        var completedCount = allLessons.Count(l => completedLessonDict.ContainsKey(l.Id));
        var progress = totalLessons > 0 ? (int)Math.Round((double)completedCount / totalLessons * 100) : 0;

        var dto = new SubjectStudyDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Description = subject.Description,
            IsEnrolled = isEnrolled,
            TotalLessons = totalLessons,
            CompletedLessons = completedCount,
            ProgressPercentage = progress,
            Teachers = subject.Teachers.Select(t => new TeacherDto
            {
                Id = t.Id,
                UserName = t.UserName,
                Name = t.Name,
                Email = t.Email
            }).ToList(),
            SubjectExams = subject.Exams.Select(e => new ExamSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                SubjectId = e.SubjectId,
                SubjectName = subject.Name,
                DurationInMinutes = e.DurationInMinutes,
                PassScore = e.PassScore,
                MaxScore = e.MaxScore,
                IsPublished = e.IsPublished
            }).ToList()
        };

        foreach (var ch in subject.Chapters.OrderBy(c => c.Id))
        {
            var chDto = new ChapterStudyDto
            {
                Id = ch.Id,
                Name = ch.Name,
                Description = ch.Description,
                SubjectId = ch.SubjectId,
                Lessons = ch.Lessons.OrderBy(l => l.Id).Select(l => new LessonStudyDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    ChapterId = l.ChapterId,
                    ChapterName = ch.Name,
                    IsCompleted = completedLessonDict.ContainsKey(l.Id),
                    CompletedAt = completedLessonDict.GetValueOrDefault(l.Id)
                }).ToList(),
                ChapterExams = ch.Exams.Select(e => new ExamSummaryDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    SubjectId = e.SubjectId,
                    SubjectName = subject.Name,
                    ChapterId = ch.Id,
                    ChapterName = ch.Name,
                    DurationInMinutes = e.DurationInMinutes,
                    PassScore = e.PassScore,
                    MaxScore = e.MaxScore,
                    IsPublished = e.IsPublished
                }).ToList()
            };

            chDto.CompletedLessons = chDto.Lessons.Count(l => l.IsCompleted);
            dto.Chapters.Add(chDto);
        }

        return dto;
    }

    /// <summary>
    /// Lấy chi tiết nội dung một bài học và các bài học liền trước / liền sau
    /// </summary>
    public async Task<LessonDetailDto> GetLessonDetailAsync(long lessonId)
    {
        var userId = _currentUser.Id;

        var lesson = await (await _lessonRepository.GetQueryableAsync())
            .Include(l => l.Chapter)
                .ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(l => l.Id == lessonId)
            ?? throw new UserFriendlyException("Không tìm thấy bài học.");

        var chapter = lesson.Chapter;
        var allLessonsInChapter = await (await _lessonRepository.GetQueryableAsync())
            .Where(l => l.ChapterId == chapter.Id)
            .OrderBy(l => l.Id)
            .ToListAsync();

        var currentIndex = allLessonsInChapter.FindIndex(l => l.Id == lessonId);
        Lesson? prevLesson = currentIndex > 0 ? allLessonsInChapter[currentIndex - 1] : null;
        Lesson? nextLesson = currentIndex >= 0 && currentIndex < allLessonsInChapter.Count - 1
            ? allLessonsInChapter[currentIndex + 1]
            : null;

        var isCompleted = false;
        DateTime? completedAt = null;

        if (userId.HasValue)
        {
            var progress = await (await _learningProgressRepository.GetQueryableAsync())
                .FirstOrDefaultAsync(lp => lp.UserId == userId.Value && lp.LessonId == lessonId);
            if (progress != null && progress.IsCompleted)
            {
                isCompleted = true;
                completedAt = progress.CompletedAt;
            }
        }

        return new LessonDetailDto
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Content = lesson.Content,
            ChapterId = chapter.Id,
            ChapterName = chapter.Name,
            SubjectId = chapter.SubjectId,
            SubjectName = chapter.Subject.Name,
            IsCompleted = isCompleted,
            CompletedAt = completedAt,
            PreviousLessonId = prevLesson?.Id,
            PreviousLessonTitle = prevLesson?.Title,
            NextLessonId = nextLesson?.Id,
            NextLessonTitle = nextLesson?.Title
        };
    }

    /// <summary>
    /// Đánh dấu bài học đã hoàn thành hoặc chưa hoàn thành
    /// </summary>
    public async Task<bool> ToggleLessonCompleteAsync(long lessonId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Vui lòng đăng nhập để lưu tiến độ học tập.");

        var progress = await _learningProgressRepository.FirstOrDefaultAsync(lp => lp.UserId == userId && lp.LessonId == lessonId);

        if (progress == null)
        {
            progress = new LearningProgress
            {
                UserId = userId,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            };
            await _learningProgressRepository.InsertAsync(progress, autoSave: true);
        }
        else
        {
            progress.IsCompleted = !progress.IsCompleted;
            progress.CompletedAt = progress.IsCompleted ? DateTime.UtcNow : null;
            await _learningProgressRepository.UpdateAsync(progress, autoSave: true);
        }

        return progress.IsCompleted;
    }

    /// <summary>
    /// Đăng ký học môn học
    /// </summary>
    public async Task RegisterSubjectAsync(long subjectId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Vui lòng đăng nhập để đăng ký môn học.");

        var subject = await _subjectRepository.FindAsync(subjectId)
            ?? throw new UserFriendlyException("Không tìm thấy môn học.");

        if (!subject.IsActive)
            throw new UserFriendlyException("Môn học này hiện chưa mở.");

        var enrollment = await _enrollmentSubjectRepository.FirstOrDefaultAsync(e => e.UserId == userId && e.SubjectId == subjectId);

        if (enrollment != null)
        {
            enrollment.IsActive = true;
            enrollment.RegisteredAt = DateTime.UtcNow;
            enrollment.UnregisteredAt = null;
            await _enrollmentSubjectRepository.UpdateAsync(enrollment, autoSave: true);
        }
        else
        {
            await _enrollmentSubjectRepository.InsertAsync(new EnrollmentSubject
            {
                UserId = userId,
                SubjectId = subjectId,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            }, autoSave: true);
        }
    }

    /// <summary>
    /// Hủy đăng ký môn học
    /// </summary>
    public async Task UnregisterSubjectAsync(long subjectId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Vui lòng đăng nhập để thực hiện.");

        var enrollment = await _enrollmentSubjectRepository.FirstOrDefaultAsync(e => e.UserId == userId && e.SubjectId == subjectId && e.IsActive);

        if (enrollment == null)
        {
            throw new UserFriendlyException("Không tìm thấy thông tin đăng ký môn học này.");
        }

        // Kiểm tra backend: Không cho phép hủy nếu học viên đã hoàn thành bất kỳ bài học nào trong môn này
        var lessonIdsQuery = (await _lessonRepository.GetQueryableAsync())
            .Where(l => l.Chapter.SubjectId == subjectId)
            .Select(l => l.Id);

        var hasCompleted = await (await _learningProgressRepository.GetQueryableAsync())
            .AnyAsync(lp => lp.UserId == userId && lp.IsCompleted && lessonIdsQuery.Contains(lp.LessonId));

        if (hasCompleted)
        {
            throw new UserFriendlyException("Không thể hủy đăng ký vì bạn đã hoàn thành ít nhất một bài học trong môn này.");
        }

        enrollment.IsActive = false;
        enrollment.UnregisteredAt = DateTime.UtcNow;
        await _enrollmentSubjectRepository.UpdateAsync(enrollment, autoSave: true);
    }
}