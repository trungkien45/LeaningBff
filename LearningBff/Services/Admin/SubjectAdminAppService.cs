using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Volo.Abp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearningBff.Services;

[Authorize(Roles = LearningBffConsts.Admin)]
public class SubjectAdminAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;
    private readonly SubjectAppService _subjectAppService;

    public SubjectAdminAppService(
        LearningBffDbContext db,
        SubjectAppService subjectAppService)
    {
        _db = db;
        _subjectAppService = subjectAppService;
    }

    /// <summary>
    /// Lấy thống kê tổng quan các môn học cho Admin
    /// </summary>
    public async Task<(int Total, int Active, int Assigned, int Unassigned)> GetAdminStatsAsync()
    {
        var list = await _db.Subjects
            .AsNoTracking()
            .Select(s => new { s.IsActive, HasTeachers = s.Teachers.Any() })
            .ToListAsync();

        var total = list.Count;
        var active = list.Count(s => s.IsActive);
        var assigned = list.Count(s => s.HasTeachers);
        var unassigned = total - assigned;

        return (total, active, assigned, unassigned);
    }

    /// <summary>
    /// Tạo môn học mới và gán danh sách giảng viên ban đầu
    /// </summary>
    public async Task<SubjectDto> CreateAsync(CreateSubjectDto input)
    {
        var trimmedName = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new UserFriendlyException("Tên môn học không được để trống.");
        }

        var exists = await _db.Subjects.AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower());
        if (exists)
        {
            throw new UserFriendlyException($"Môn học '{trimmedName}' đã tồn tại trong hệ thống.");
        }

        var subject = new Subject(trimmedName, input.Description?.Trim(), input.IsActive);

        if (input.TeacherIds != null && input.TeacherIds.Any())
        {
            var teachers = await _db.Users
                .Where(u => input.TeacherIds.Contains(u.Id))
                .ToListAsync();
            subject.Teachers.AddRange(teachers);
        }

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        return await _subjectAppService.GetAsync(subject.Id);
    }

    /// <summary>
    /// Cập nhật thông tin môn học và giảng viên
    /// </summary>
    public async Task<SubjectDto> UpdateAsync(long id, UpdateSubjectDto input)
    {
        var trimmedName = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new UserFriendlyException("Tên môn học không được để trống.");
        }

        var subject = await _db.Subjects
            .Include(s => s.Teachers)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new UserFriendlyException($"Không tìm thấy môn học với mã {id}.");

        var duplicate = await _db.Subjects.AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower() && x.Id != id);
        if (duplicate)
        {
            throw new UserFriendlyException($"Môn học '{trimmedName}' đã tồn tại trong hệ thống.");
        }

        subject.Name = trimmedName;
        subject.Description = input.Description?.Trim();
        subject.IsActive = input.IsActive;

        // Cập nhật quan hệ giảng viên
        subject.Teachers.Clear();
        if (input.TeacherIds != null && input.TeacherIds.Any())
        {
            var teachers = await _db.Users
                .Where(u => input.TeacherIds.Contains(u.Id))
                .ToListAsync();
            subject.Teachers.AddRange(teachers);
        }

        await _db.SaveChangesAsync();
        return await _subjectAppService.GetAsync(subject.Id);
    }

    /// <summary>
    /// Gán hoặc bỏ gán giảng viên cho môn học
    /// </summary>
    public async Task AssignTeachersAsync(long subjectId, List<Guid> teacherIds)
    {
        var subject = await _db.Subjects
            .Include(s => s.Teachers)
            .FirstOrDefaultAsync(s => s.Id == subjectId)
            ?? throw new UserFriendlyException($"Không tìm thấy môn học với mã {subjectId}.");

        subject.Teachers.Clear();
        if (teacherIds != null && teacherIds.Any())
        {
            var teachers = await _db.Users
                .Where(u => teacherIds.Contains(u.Id))
                .ToListAsync();
            subject.Teachers.AddRange(teachers);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Xóa môn học
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        var subject = await _db.Subjects
            .Include(s => s.Teachers)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new UserFriendlyException($"Không tìm thấy môn học với mã {id}.");

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();
    }
}
