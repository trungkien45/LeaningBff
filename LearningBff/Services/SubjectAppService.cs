using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LearningBff.Entities;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using LearningBff.Dtos;

namespace LearningBff.Services;

public class SubjectAppService : LearningBffAppService
{
    private readonly IRepository<Subject, long> _subjectRepository;
    private readonly IdentityUserManager _userManager;

    public SubjectAppService(
        IRepository<Subject, long> subjectRepository,
        IdentityUserManager userManager)
    {
        _subjectRepository = subjectRepository;
        _userManager = userManager;
    }

    /// <summary>
    /// Lấy danh sách môn học có phân trang và tìm kiếm theo tên
    /// </summary>
    public async Task<PagedResultDto<SubjectDto>> GetPagedListAsync(
        string? searchName = null,
        int skipCount = 0,
        int maxResultCount = 10,
        bool? isActive = null)
    {
        var query = await _subjectRepository.GetQueryableAsync();
        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            var term = searchName.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var subjects = await query
            .Include(s => s.Teachers)
            .Include(s => s.Questions)
            .Include(s => s.Exams)
            .Include(s => s.EnrollmentSubjects)
            .OrderBy(s => s.Name)
            .Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 100))
            .ToListAsync();

        var items = subjects.Select(s => new SubjectDto
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

        return new PagedResultDto<SubjectDto>(totalCount, items);
    }

    /// <summary>
    /// Lấy danh sách tất cả môn học kèm giảng viên phụ trách và thống kê
    /// </summary>
    public async Task<List<SubjectDto>> GetListAsync(bool? isActive = null)
    {
        var query = await _subjectRepository.GetQueryableAsync();
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
        var s = await (await _subjectRepository.GetQueryableAsync())
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
        //var admins = await _userManager.GetUsersInRoleAsync("admin");
        //var combined = teachers.Concat(admins).DistinctBy(u => u.Id).ToList();

        // Nếu chưa có user gán role, lấy tối đa 50 user hoạt động để chọn
        //if (!combined.Any())
        //{
        //    combined = await _db.Users.AsNoTracking().Where(u => u.IsActive).Take(50).ToListAsync();
        //}

        return teachers.Select(u => new TeacherSimpleDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Name = u.Name,
            Surname = u.Surname,
            Email = u.Email
        }).OrderBy(t => t.Name ?? t.UserName).ToList();
    }
}
