using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Admin.Subjects;

[Authorize(Roles = "admin")]
public class IndexModel : PageModel
{
    private const int PageSize = 10;
    private readonly SubjectAppService _subjectService;

    public List<SubjectDto> Subjects { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? SuccessMessage { get; set; }
    public long TotalCount { get; set; }
    public string? ErrorMessage   { get; set; }

    public IndexModel(SubjectAppService subjectService)
    {
        _subjectService = subjectService;
    }

    private async Task LoadAsync(int page)
    {
        var safePage = Math.Clamp(page, 1, 1_000_000);
        var pageData = await _subjectService.GetPageAsync((safePage - 1) * PageSize, PageSize);
        TotalCount = pageData.TotalCount;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        CurrentPage = Math.Clamp(safePage, 1, TotalPages);
        if (CurrentPage != safePage)
            pageData = await _subjectService.GetPageAsync((CurrentPage - 1) * PageSize, PageSize);
        Subjects = pageData.Items.ToList();
        SuccessMessage = TempData["Success"]?.ToString();
        ErrorMessage   = TempData["Error"]?.ToString();
    }

    public async Task OnGetAsync(int page = 1)
    {
        await LoadAsync(page);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string Name, string? Description, bool IsActive = false)
    {
        // Checkbox unchecked sends nothing → default false, but we want true
        // We handle it via the hidden field trick in the view
        try
        {
            await _subjectService.CreateAsync(new CreateSubjectDto
            {
                Name        = Name,
                Description = Description,
                IsActive    = IsActive
            });
            TempData["Success"] = $"Đã tạo môn học '{Name}'.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        long Id, string Name, string? Description, bool IsActive = false)
    {
        try
        {
            await _subjectService.UpdateAsync(Id, new UpdateSubjectDto
            {
                Name        = Name,
                Description = Description,
                IsActive    = IsActive
            });
            TempData["Success"] = $"Đã cập nhật môn học '{Name}'.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long Id)
    {
        try
        {
            await _subjectService.DeleteAsync(Id);
            TempData["Success"] = "Đã xóa môn học.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }
}
