using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;
using LearningBff.Data;
using LearningBff.Entities;

namespace LearningBff.Pages.Admin;

public class AdminSubjectSummaryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TeacherCount { get; set; }
    public List<string> TeacherNames { get; set; } = new();
    public int ExamCount { get; set; }
    public int QuestionCount { get; set; }
    public int StudentCount { get; set; }
}

public class AdminUserSummaryDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreationTime { get; set; }
}

[Authorize(Roles = "admin,Admin")]
public class IndexModel : AbpPageModel
{
    private readonly LearningBffDbContext _db;
    private readonly IdentityUserManager _userManager;

    public int TotalSubjects { get; set; }
    public int ActiveSubjects { get; set; }
    public int InactiveSubjects { get; set; }

    public int TotalTeachers { get; set; }

    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }

    public int TotalRoles { get; set; }
    public int TotalExams { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalSubmissions { get; set; }

    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string OsDescription { get; set; } = string.Empty;
    public string FrameworkDescription { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }

    public List<AdminSubjectSummaryDto> SubjectsSummary { get; set; } = new();
    public List<AdminUserSummaryDto> RecentUsers { get; set; } = new();

    public IndexModel(LearningBffDbContext db, IdentityUserManager userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task OnGetAsync()
    {
        // 1. Thống kê môn học
        TotalSubjects = await _db.Subjects.CountAsync();
        ActiveSubjects = await _db.Subjects.CountAsync(s => s.IsActive);
        InactiveSubjects = TotalSubjects - ActiveSubjects;

        // 2. Thống kê Users (Total, Active, Inactive)
        TotalUsers = await _db.Users.CountAsync();
        ActiveUsers = await _db.Users.CountAsync(u => u.IsActive);
        InactiveUsers = await _db.Users.CountAsync(u => !u.IsActive);

        // 3. Thống kê Giáo viên (Teachers)
        var teacherUsers = await _userManager.GetUsersInRoleAsync("teacher");
        var adminUsers = await _userManager.GetUsersInRoleAsync("admin");
        var distinctTeachers = teacherUsers.Concat(adminUsers).DistinctBy(u => u.Id).ToList();
        TotalTeachers = distinctTeachers.Count;

        // 4. Thống kê Đề thi, Câu hỏi, Lượt nộp
        TotalRoles = await _db.Roles.CountAsync();
        TotalExams = await _db.Exams.CountAsync();
        TotalQuestions = await _db.Questions.CountAsync();
        TotalSubmissions = await _db.ExamResults.CountAsync(r => r.Status == ExamResultStatus.Submitted);

        // 5. Thông tin admin hiện tại & Hệ thống
        AdminUserName = CurrentUser.UserName ?? "Admin";
        AdminEmail = CurrentUser.Email ?? "admin@abp.io";
        OsDescription = RuntimeInformation.OSDescription;
        FrameworkDescription = RuntimeInformation.FrameworkDescription;
        MachineName = Environment.MachineName;
        ServerTime = DateTime.UtcNow;

        // 6. Danh sách tóm tắt môn học
        var subjects = await _db.Subjects.AsNoTracking()
            .Include(s => s.Teachers)
            .Include(s => s.Exams)
            .Include(s => s.Questions)
            .Include(s => s.EnrollmentSubjects)
            .OrderByDescending(s => s.CreationTime)
            .Take(6)
            .ToListAsync();

        SubjectsSummary = subjects.Select(s => new AdminSubjectSummaryDto
        {
            Id = s.Id,
            Name = s.Name,
            IsActive = s.IsActive,
            TeacherCount = s.Teachers.Count,
            TeacherNames = s.Teachers.Select(t => !string.IsNullOrEmpty(t.Name) ? t.Name : t.UserName).ToList(),
            ExamCount = s.Exams.Count,
            QuestionCount = s.Questions.Count,
            StudentCount = s.EnrollmentSubjects.Count(e => e.IsActive)
        }).ToList();

        // 7. Người dùng mới nhất
        RecentUsers = await _db.Users.AsNoTracking()
            .OrderByDescending(u => u.CreationTime)
            .Take(6)
            .Select(u => new AdminUserSummaryDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Name = u.Name,
                Email = u.Email,
                IsActive = u.IsActive,
                CreationTime = u.CreationTime
            })
            .ToListAsync();
    }
}
