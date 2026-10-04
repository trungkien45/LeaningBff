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

public class LearningAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LearningAppService(LearningBffDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Danh sách các môn học mà học viên hiện tại đã đăng ký
    /// </summary>
    public async Task<List<SubjectCardDto>> GetMyCoursesAsync()
    {
        var userId = _currentUser.Id;
        if (!userId.HasValue)
        {
            return new List<SubjectCardDto>();
        }

        var enrollments = await _db.EnrollmentSubjects.AsNoTracking()
            .Where(e => e.UserId == userId.Value && e.IsActive)
            .Include(e => e.Subject)
                .ThenInclude(s => s!.Chapters)
                    .ThenInclude(c => c.Lessons)
            .Include(e => e.Subject)
                .ThenInclude(s => s!.Teachers)
            .Include(e => e.Subject)
                .ThenInclude(s => s!.Exams)
            .OrderByDescending(e => e.RegisteredAt)
            .ToListAsync();

        var subjectIds = enrollments.Select(e => e.SubjectId).ToList();

        // Get completed lessons for this user
        var completedLessonIds = await _db.LearningProgesses.AsNoTracking()
            .Where(lp => lp.UserId == userId.Value && lp.IsCompleted)
            .Select(lp => lp.LessonId)
            .ToHashSetAsync();

        var result = new List<SubjectCardDto>();
        foreach (var enrollment in enrollments)
        {
            var s = enrollment.Subject;
            if (s == null) continue;

            var allLessons = s.Chapters.SelectMany(c => c.Lessons).ToList();
            var totalLessons = allLessons.Count;
            var completedCount = allLessons.Count(l => completedLessonIds.Contains(l.Id));
            var progress = totalLessons > 0 ? (int)Math.Round((double)completedCount / totalLessons * 100) : 0;

            result.Add(new SubjectCardDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                IsEnrolled = true,
                EnrolledAt = enrollment.RegisteredAt,
                TotalChapters = s.Chapters.Count,
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

        return result;
    }

    /// <summary>
    /// Tất cả môn học cho học viên khám phá và đăng ký
    /// </summary>
    public async Task<List<SubjectCardDto>> GetAllCoursesAsync()
    {
        var userId = _currentUser.Id;

        var subjects = await _db.Subjects.AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Teachers)
            .Include(s => s.Chapters)
                .ThenInclude(c => c.Lessons)
            .Include(s => s.Exams)
            .OrderBy(s => s.Name)
            .ToListAsync();

        HashSet<long> enrolledSubjectIds = new();
        HashSet<long> completedLessonIds = new();

        if (userId.HasValue)
        {
            enrolledSubjectIds = (await _db.EnrollmentSubjects.AsNoTracking()
                .Where(e => e.UserId == userId.Value && e.IsActive)
                .Select(e => e.SubjectId)
                .ToListAsync()).ToHashSet();

            completedLessonIds = (await _db.LearningProgesses.AsNoTracking()
                .Where(lp => lp.UserId == userId.Value && lp.IsCompleted)
                .Select(lp => lp.LessonId)
                .ToListAsync()).ToHashSet();
        }

        var result = new List<SubjectCardDto>();
        foreach (var s in subjects)
        {
            var allLessons = s.Chapters.SelectMany(c => c.Lessons).ToList();
            var totalLessons = allLessons.Count;
            var completedCount = allLessons.Count(l => completedLessonIds.Contains(l.Id));
            var progress = totalLessons > 0 ? (int)Math.Round((double)completedCount / totalLessons * 100) : 0;

            result.Add(new SubjectCardDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                IsEnrolled = enrolledSubjectIds.Contains(s.Id),
                TotalChapters = s.Chapters.Count,
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

        return result;
    }

    /// <summary>
    /// Lấy chi tiết cây học tập của một môn học (Chương, Bài học, Đề thi, Tiến độ)
    /// </summary>
    public async Task<SubjectStudyDto> GetCourseStudyAsync(long subjectId)
    {
        var userId = _currentUser.Id;

        var subject = await _db.Subjects.AsNoTracking()
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
            isEnrolled = await _db.EnrollmentSubjects.AnyAsync(e =>
                e.UserId == userId.Value && e.SubjectId == subjectId && e.IsActive);

            var progresses = await _db.LearningProgesses.AsNoTracking()
                .Where(lp => lp.UserId == userId.Value && lp.IsCompleted)
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

        var lesson = await _db.Lessons.AsNoTracking()
            .Include(l => l.Chapter)
                .ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(l => l.Id == lessonId)
            ?? throw new UserFriendlyException("Không tìm thấy bài học.");

        var chapter = lesson.Chapter;
        var allLessonsInChapter = await _db.Lessons.AsNoTracking()
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
            var progress = await _db.LearningProgesses.AsNoTracking()
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

        var progress = await _db.LearningProgesses
            .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.LessonId == lessonId);

        if (progress == null)
        {
            progress = new LearningProgress
            {
                UserId = userId,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            };
            _db.LearningProgesses.Add(progress);
        }
        else
        {
            progress.IsCompleted = !progress.IsCompleted;
            progress.CompletedAt = progress.IsCompleted ? DateTime.UtcNow : null;
        }

        await _db.SaveChangesAsync();
        return progress.IsCompleted;
    }

    /// <summary>
    /// Đăng ký học môn học
    /// </summary>
    public async Task RegisterSubjectAsync(long subjectId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Vui lòng đăng nhập để đăng ký môn học.");

        var subject = await _db.Subjects.FindAsync(subjectId)
            ?? throw new UserFriendlyException("Không tìm thấy môn học.");

        if (!subject.IsActive)
            throw new UserFriendlyException("Môn học này hiện chưa mở.");

        var enrollment = await _db.EnrollmentSubjects
            .FirstOrDefaultAsync(e => e.UserId == userId && e.SubjectId == subjectId);

        if (enrollment != null)
        {
            enrollment.IsActive = true;
            enrollment.RegisteredAt = DateTime.UtcNow;
            enrollment.UnregisteredAt = null;
        }
        else
        {
            _db.EnrollmentSubjects.Add(new EnrollmentSubject
            {
                UserId = userId,
                SubjectId = subjectId,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Hủy đăng ký môn học
    /// </summary>
    public async Task UnregisterSubjectAsync(long subjectId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Vui lòng đăng nhập để thực hiện.");

        var enrollment = await _db.EnrollmentSubjects
            .FirstOrDefaultAsync(e => e.UserId == userId && e.SubjectId == subjectId && e.IsActive);

        if (enrollment != null)
        {
            enrollment.IsActive = false;
            enrollment.UnregisteredAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
