using LearningBff.Entities;
using LearningBff.Dtos;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LearningBff.Services.Teacher;

public class LessonTeacherAppService : TeacherBaseAppService
{
    private readonly IRepository<Chapter, long> _chapterRepository;
    private readonly IRepository<Lesson, long> _lessonRepository;

    public LessonTeacherAppService(
        IRepository<Subject, long> subjectRepository,
        IRepository<Chapter, long> chapterRepository,
        IRepository<Lesson, long> lessonRepository,
        ICurrentUser currentUser) : base(subjectRepository, currentUser)
    {
        _chapterRepository = chapterRepository;
        _lessonRepository = lessonRepository;
    }
    public async Task<(List<LessonDto> Lessons, int TotalCount)> GetLessonsOfChapterAsync(long chapterId, string searchName = "",
        int page = 1,
        int pageSize = 9)
    {
        var userId = _currentUser.Id;
        var chapter = await (await _chapterRepository.GetQueryableAsync())
            .Where(c => c.Id == chapterId && c.Subject.Teachers.Any(t => t.Id == userId))
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync();
        if (chapter == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy chương học hoặc bạn không có quyền truy cập.");
        }
        var query = chapter.Lessons.AsQueryable()
            .Where(l => string.IsNullOrEmpty(searchName) || l.Title.Contains(searchName))
            .OrderBy(l => l.CreationTime);
        var totalCount = await query.CountAsync();
        var lessons = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (lessons.Select(l => new LessonDto
        {
            Id = l.Id,
            Title = l.Title,
            ChapterId = l.ChapterId,
            CreationTime = l.CreationTime
        }).ToList(), totalCount);
    }
    public async Task<LessonDto> GetLessonDetailAsync(long lessonId)
    {
        var userId = _currentUser.Id;
        var lesson = await (await _lessonRepository.GetQueryableAsync())
            .Where(l => l.Id == lessonId && l.Chapter.Subject.Teachers.Any(t => t.Id == userId))
            .FirstOrDefaultAsync();
        if (lesson == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy bài học hoặc bạn không có quyền truy cập.");
        }
        return new LessonDto
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Content = lesson.Content,
            ChapterId = lesson.ChapterId,
            CreationTime = lesson.CreationTime
        };
    }
    public async Task<LessonDto> AddLessonToChapterAsync(long chapterId, CreateLessonDto input)
    {
        var userId = _currentUser.Id;
        var chapter = await (await _chapterRepository.GetQueryableAsync())
            .Where(c => c.Id == chapterId && c.Subject.Teachers.Any(t => t.Id == userId))
            .FirstOrDefaultAsync();
        if (chapter == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy chương học hoặc bạn không có quyền truy cập.");
        }
        var trimmedTitle = input.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            throw new UserFriendlyException("Tiêu đề bài học không được để trống.");
        }
        var exists = await _lessonRepository.AnyAsync(l => l.ChapterId == chapterId && l.Title.ToLower() == trimmedTitle.ToLower());
        if (exists)
        {
            throw new UserFriendlyException($"Bài học '{trimmedTitle}' đã tồn tại trong chương học này.");
        }
        var lesson = new Lesson
        {
            Title = trimmedTitle,
            Content = input.Content?.Trim(),
            ChapterId = chapterId
        };
        await _lessonRepository.InsertAsync(lesson, autoSave: true);
        return new LessonDto
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Content = lesson.Content,
            ChapterId = lesson.ChapterId,
            CreationTime = lesson.CreationTime
        };
    }
    public async Task<LessonDto> UpdateLessonAsync(long lessonId, UpdateLessonDto input)
    {
        var userId = _currentUser.Id;
        var lesson = await (await _lessonRepository.GetQueryableAsync())
            .Where(l => l.Id == lessonId && l.Chapter.Subject.Teachers.Any(t => t.Id == userId))
            .FirstOrDefaultAsync();
        if (lesson == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy bài học hoặc bạn không có quyền truy cập.");
        }
        var trimmedTitle = input.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            throw new UserFriendlyException("Tiêu đề bài học không được để trống.");
        }
        var exists = await _lessonRepository.AnyAsync(l => l.ChapterId == lesson.ChapterId && l.Title.ToLower() == trimmedTitle.ToLower() && l.Id != lessonId);
        if (exists)
        {
            throw new UserFriendlyException($"Bài học '{trimmedTitle}' đã tồn tại trong chương học này.");
        }
        lesson.Title = trimmedTitle;
        lesson.Content = input.Content?.Trim();
        await _lessonRepository.UpdateAsync(lesson, autoSave: true);
        return new LessonDto
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Content = lesson.Content,
            ChapterId = lesson.ChapterId,
            CreationTime = lesson.CreationTime
        };
    }
    public async Task<long> DeleteLessonAsync(long lessonId)
    {
        var userId = _currentUser.Id;
        var lesson = await (await _lessonRepository.GetQueryableAsync())
            .Where(l => l.Id == lessonId && l.Chapter.Subject.Teachers.Any(t => t.Id == userId))
            .FirstOrDefaultAsync();
        if (lesson == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy bài học hoặc bạn không có quyền truy cập.");
        }
        await _lessonRepository.DeleteAsync(lesson, autoSave: true);
        return lesson.Id;
    }
}