using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Volo.Abp;
using Volo.Abp.Identity;

namespace LearningBff.Services;

public class SubjectAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;
    private readonly IdentityUserManager _userManager;

    public SubjectAppService(
        LearningBffDbContext db,
        IdentityUserManager userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>
    /// Lấy danh sách tất cả môn học kèm giảng viên phụ trách và thống kê
    /// </summary>
    public async Task<List<SubjectDto>> GetListAsync(bool? isActive = null)
    {
        var query = _db.Subjects.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        var subjects = await query
            .Include(s => s.Teachers)
            .Include(s => s.Questions)
            .Include(s => s.Exams)
            .Include(s => s.EnrollmentSubjects)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return subjects.Select(s => new SubjectDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            IsActive = s.IsActive,
            CreationTime = s.CreationTime,
            QuestionCount = s.Questions.Count,
            ExamCount = s.Exams.Count,
            StudentCount = s.EnrollmentSubjects.Count(e => e.IsActive),
            Teachers = s.Teachers.Select(t => new TeacherSimpleDto
            {
                Id = t.Id,
                UserName = t.UserName,
                Name = t.Name,
                Surname = t.Surname,
                Email = t.Email
            }).ToList()
        }).ToList();
    }

    /// <summary>
    /// Lấy chi tiết một môn học theo ID
    /// </summary>
    public async Task<SubjectDto> GetAsync(long id)
    {
        var s = await _db.Subjects
            .AsNoTracking()
            .Include(x => x.Teachers)
            .Include(x => x.Questions)
            .Include(x => x.Exams)
            .Include(x => x.EnrollmentSubjects)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new UserFriendlyException($"Không tìm thấy môn học với mã {id}.");

        return new SubjectDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            IsActive = s.IsActive,
            CreationTime = s.CreationTime,
            QuestionCount = s.Questions.Count,
            ExamCount = s.Exams.Count,
            StudentCount = s.EnrollmentSubjects.Count(e => e.IsActive),
            Teachers = s.Teachers.Select(t => new TeacherSimpleDto
            {
                Id = t.Id,
                UserName = t.UserName,
                Name = t.Name,
                Surname = t.Surname,
                Email = t.Email
            }).ToList()
        };
    }

    /// <summary>
    /// Lấy danh sách giảng viên để phục vụ gán môn học
    /// </summary>
    public async Task<List<TeacherSimpleDto>> GetAvailableTeachersAsync()
    {
        var teachers = await _userManager.GetUsersInRoleAsync("teacher");
        var admins = await _userManager.GetUsersInRoleAsync("admin");
        var combined = teachers.Concat(admins).DistinctBy(u => u.Id).ToList();

        // Nếu chưa có user gán role, lấy tối đa 50 user hoạt động để chọn
        if (!combined.Any())
        {
            combined = await _db.Users.AsNoTracking().Where(u => u.IsActive).Take(50).ToListAsync();
        }

        return combined.Select(u => new TeacherSimpleDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Name = u.Name,
            Surname = u.Surname,
            Email = u.Email
        }).OrderBy(t => t.Name ?? t.UserName).ToList();
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

        return await GetAsync(subject.Id);
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
        return await GetAsync(subject.Id);
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
