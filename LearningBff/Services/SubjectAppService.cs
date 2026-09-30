using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LearningBff.Services;

public class SubjectAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;

    public SubjectAppService(LearningBffDbContext db)
    {
        _db = db;
    }

    public async Task<List<SubjectDto>> GetListAsync(bool? isActive = null)
    {
        var query = _db.Subjects.AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var subjects = await query
            .Include(s => s.Questions)
            .Include(s => s.Exams)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return subjects.Select(s => new SubjectDto
        {
            Id           = s.Id,
            Name         = s.Name,
            Description  = s.Description,
            IsActive     = s.IsActive,
            QuestionCount = s.Questions.Count,
            ExamCount    = s.Exams.Count
        }).ToList();
    }

    public async Task<PagedResultDto<SubjectDto>> GetPageAsync(int skipCount, int maxResultCount)
    {
        var query = _db.Subjects.AsNoTracking();
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(subject => subject.Name)
            .ThenBy(subject => subject.Id)
            .Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 100))
            .Select(subject => new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Description = subject.Description,
                IsActive = subject.IsActive,
                QuestionCount = _db.Questions.Count(question => question.SubjectId == subject.Id),
                ExamCount = _db.Exams.Count(exam => exam.SubjectId == subject.Id)
            })
            .ToListAsync();

        return new PagedResultDto<SubjectDto>(totalCount, items);
    }

    public async Task<SubjectDto> GetAsync(long id)
    {
        var s = await _db.Subjects
            .AsNoTracking()
            .Include(x => x.Questions)
            .Include(x => x.Exams)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new UserFriendlyException($"Không tìm thấy môn học ID={id}");

        return new SubjectDto
        {
            Id            = s.Id,
            Name          = s.Name,
            Description   = s.Description,
            IsActive      = s.IsActive,
            QuestionCount = s.Questions.Count,
            ExamCount     = s.Exams.Count
        };
    }

    public async Task<SubjectDto> CreateAsync(CreateSubjectDto input)
    {
        var exists = await _db.Subjects.AnyAsync(x => x.Name == input.Name);
        if (exists)
            throw new UserFriendlyException($"Môn học '{input.Name}' đã tồn tại.");

        var subject = new Subject(input.Name, input.Description, input.IsActive);
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();
        return await GetAsync(subject.Id);
    }

    public async Task<SubjectDto> UpdateAsync(long id, UpdateSubjectDto input)
    {
        var subject = await _db.Subjects.FindAsync(id)
            ?? throw new UserFriendlyException($"Không tìm thấy môn học ID={id}");

        var duplicate = await _db.Subjects.AnyAsync(x => x.Name == input.Name && x.Id != id);
        if (duplicate)
            throw new UserFriendlyException($"Môn học '{input.Name}' đã tồn tại.");

        subject.Name        = input.Name;
        subject.Description = input.Description;
        subject.IsActive    = input.IsActive;
        await _db.SaveChangesAsync();
        return await GetAsync(subject.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var subject = await _db.Subjects.FindAsync(id)
            ?? throw new UserFriendlyException($"Không tìm thấy môn học ID={id}");

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();
    }
}
