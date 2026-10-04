using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Admin;

[Authorize(Roles = "admin,Admin")]
public class SubjectsModel : AbpPageModel
{
    private readonly SubjectAppService _subjectAppService;
    private readonly SubjectAdminAppService _subjectAdminAppService;

    public SubjectsModel(
        SubjectAppService subjectAppService,
        SubjectAdminAppService subjectAdminAppService)
    {
        _subjectAppService = subjectAppService;
        _subjectAdminAppService = subjectAdminAppService;
    }

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<TeacherSimpleDto> AvailableTeachers { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 10;
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }

    public int StartIndex => TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int EndIndex => (int)Math.Min(CurrentPage * PageSize, TotalCount);

    public int StartPage
    {
        get
        {
            const int maxDisplay = 5;
            int start = Math.Max(1, CurrentPage - maxDisplay / 2);
            int end = Math.Min(TotalPages, start + maxDisplay - 1);
            if (end - start + 1 < maxDisplay)
            {
                start = Math.Max(1, end - maxDisplay + 1);
            }
            return start;
        }
    }

    public int EndPage
    {
        get
        {
            const int maxDisplay = 5;
            int start = Math.Max(1, CurrentPage - maxDisplay / 2);
            int end = Math.Min(TotalPages, start + maxDisplay - 1);
            if (end - start + 1 < maxDisplay)
            {
                end = Math.Min(TotalPages, start + maxDisplay - 1);
            }
            return end;
        }
    }

    public int TotalSystemSubjects { get; set; }
    public int ActiveSystemSubjects { get; set; }
    public int AssignedSystemSubjects { get; set; }
    public int UnassignedSystemSubjects { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? search = null, int currentPage = 1)
    {
        Search = search;
        CurrentPage = currentPage < 1 ? 1 : currentPage;

        var stats = await _subjectAdminAppService.GetAdminStatsAsync();
        TotalSystemSubjects = stats.Total;
        ActiveSystemSubjects = stats.Active;
        AssignedSystemSubjects = stats.Assigned;
        UnassignedSystemSubjects = stats.Unassigned;

        var pagedResult = await _subjectAppService.GetPagedListAsync(
            searchName: Search,
            skipCount: (CurrentPage - 1) * PageSize,
            maxResultCount: PageSize
        );

        TotalCount = pagedResult.TotalCount;
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        if (TotalPages > 0 && CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
            pagedResult = await _subjectAppService.GetPagedListAsync(
                searchName: Search,
                skipCount: (CurrentPage - 1) * PageSize,
                maxResultCount: PageSize
            );
        }

        Subjects = pagedResult.Items.ToList();
        AvailableTeachers = await _subjectAppService.GetAvailableTeachersAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string name,
        string? description,
        bool isActive = false,
        List<Guid>? teacherIds = null,
        string? search = null)
    {
        try
        {
            await _subjectAdminAppService.CreateAsync(new CreateSubjectDto
            {
                Name = name,
                Description = description,
                IsActive = isActive,
                TeacherIds = teacherIds ?? new List<Guid>()
            });

            SuccessMessage = $"Đã tạo thành công môn học '{name.Trim()}'.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { search });
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        long id,
        string name,
        string? description,
        bool isActive = false,
        List<Guid>? teacherIds = null,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _subjectAdminAppService.UpdateAsync(id, new UpdateSubjectDto
            {
                Name = name,
                Description = description,
                IsActive = isActive,
                TeacherIds = teacherIds ?? new List<Guid>()
            });

            SuccessMessage = $"Đã cập nhật môn học '{name.Trim()}'.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { search, currentPage });
    }

    public async Task<IActionResult> OnPostAssignTeachersAsync(
        long subjectId,
        List<Guid>? teacherIds = null,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _subjectAdminAppService.AssignTeachersAsync(subjectId, teacherIds ?? new List<Guid>());
            SuccessMessage = "Đã cập nhật danh sách giảng viên phụ trách môn học.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { search, currentPage });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        long id,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _subjectAdminAppService.DeleteAsync(id);
            SuccessMessage = "Đã xóa môn học thành công.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { search, currentPage });
    }
}
