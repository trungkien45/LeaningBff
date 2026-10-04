using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Admin.Subjects;

[Authorize(Roles = "admin,Admin")]
public class IndexModel : AbpPageModel
{
    private readonly SubjectAppService _subjectAppService;

    public IndexModel(SubjectAppService subjectAppService)
    {
        _subjectAppService = subjectAppService;
    }

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<TeacherSimpleDto> AvailableTeachers { get; set; } = new();
    public string? Search { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? search = null)
    {
        Search = search;
        var all = await _subjectAppService.GetListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            Subjects = all.Where(s => s.Name.ToLower().Contains(term)
                || (s.Description != null && s.Description.ToLower().Contains(term))
                || s.Teachers.Any(t => t.DisplayName.ToLower().Contains(term))).ToList();
        }
        else
        {
            Subjects = all;
        }

        AvailableTeachers = await _subjectAppService.GetAvailableTeachersAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string name,
        string? description,
        bool isActive = false,
        List<Guid>? teacherIds = null)
    {
        try
        {
            await _subjectAppService.CreateAsync(new CreateSubjectDto
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

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        long id,
        string name,
        string? description,
        bool isActive = false,
        List<Guid>? teacherIds = null)
    {
        try
        {
            await _subjectAppService.UpdateAsync(id, new UpdateSubjectDto
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

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignTeachersAsync(
        long subjectId,
        List<Guid>? teacherIds = null)
    {
        try
        {
            await _subjectAppService.AssignTeachersAsync(subjectId, teacherIds ?? new List<Guid>());
            SuccessMessage = "Đã cập nhật danh sách giảng viên phụ trách môn học.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        try
        {
            await _subjectAppService.DeleteAsync(id);
            SuccessMessage = "Đã xóa môn học thành công.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
