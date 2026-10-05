using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Users;
using Microsoft.EntityFrameworkCore;

namespace LearningBff.Services.Teacher;

public class ChappterTeacherAppService : TeacherBaseAppService
{
    public ChappterTeacherAppService(LearningBffDbContext db, ICurrentUser currentUser) : base(db, currentUser)
    {
    }

    public async Task<ChapterDto> AddChapterToSubjectAsync(long subjectId, CreateChapterDto input)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var trimmedName = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new UserFriendlyException("Tên chương học không được để trống.");
        }
        var exists = await _db.Chapters.AnyAsync(c => c.SubjectId == subjectId && c.Name.ToLower() == trimmedName.ToLower());
        if (exists)
        {
            throw new UserFriendlyException($"Chương học '{trimmedName}' đã tồn tại trong môn học này.");
        }
        var chapter = new Chapter(trimmedName, subjectId, input.Description?.Trim());
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();
        return new ChapterDto
        {
            Id = chapter.Id,
            Name = chapter.Name,
            Description = chapter.Description,
            SubjectId = chapter.SubjectId,
            CreationTime = chapter.CreationTime
        };
    }
    public async Task<(List<ChapterDto> Chapters, int TotalCount)> GetChaptersOfSubjectAsync(long subjectId, string searchName = "",
        int page = 1,
        int pageSize = 9)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var query = _db.Chapters.AsNoTracking()
            .Where(c => c.SubjectId == subjectId && (string.IsNullOrEmpty(searchName) || c.Name.Contains(searchName)))
            .OrderBy(c => c.CreationTime);
        var totalCount = await query.CountAsync();
        var chapters = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (chapters.Select(c => new ChapterDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            SubjectId = c.SubjectId,
            CreationTime = c.CreationTime
        }).ToList(), totalCount);
    }
    public async Task<ChapterDto> GetChapterDetailAsync(long subjectId, long chapterId)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var chapter = await _db.Chapters.AsNoTracking()
            .Where(c => c.SubjectId == subjectId && c.Id == chapterId)
            .FirstOrDefaultAsync();
        if (chapter == null)
        {
            throw new UserFriendlyException("Không tìm thấy chương học.");
        }
        return new ChapterDto
        {
            Id = chapter.Id,
            Name = chapter.Name,
            Description = chapter.Description,
            SubjectId = chapter.SubjectId,
            CreationTime = chapter.CreationTime
        };
    }
    public async Task<ChapterDto> UpdateChapterAsync(long subjectId, long chapterId, UpdateChapterDto input)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var chapter = await _db.Chapters
            .Where(c => c.SubjectId == subjectId && c.Id == chapterId)
            .FirstOrDefaultAsync();
        if (chapter == null)
        {
            throw new UserFriendlyException("Không tìm thấy chương học.");
        }
        var trimmedName = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new UserFriendlyException("Tên chương học không được để trống.");
        }
        var exists = await _db.Chapters.AnyAsync(c => c.SubjectId == subjectId && c.Name.ToLower() == trimmedName.ToLower() && c.Id != chapterId);
        if (exists)
        {
            throw new UserFriendlyException($"Chương học '{trimmedName}' đã tồn tại trong môn học này.");
        }
        chapter.Name = trimmedName;
        chapter.Description = input.Description?.Trim();
        await _db.SaveChangesAsync();
        return new ChapterDto
        {
            Id = chapter.Id,
            Name = chapter.Name,
            Description = chapter.Description,
            SubjectId = chapter.SubjectId,
            CreationTime = chapter.CreationTime
        };
    }
    public async Task<long> DeleteChapterAsync(long subjectId, long chapterId)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var chapter = await _db.Chapters
            .Where(c => c.SubjectId == subjectId && c.Id == chapterId)
            .FirstOrDefaultAsync();
        if (chapter == null)
        {
            throw new UserFriendlyException("Không tìm thấy chương học.");
        }
        _db.Chapters.Remove(chapter);
        await _db.SaveChangesAsync();
        return chapter.Id;
    }
}
