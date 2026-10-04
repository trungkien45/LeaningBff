using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Subjects;

public class IndexModel : AbpPageModel
{
    private readonly LearningAppService _learningAppService;
    private readonly ICurrentUser _currentUser;

    public IndexModel(LearningAppService learningAppService, ICurrentUser currentUser)
    {
        _learningAppService = learningAppService;
        _currentUser = currentUser;
    }

    public List<SubjectCardDto> Subjects { get; set; } = new();
    public string? Search { get; set; }
    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public async Task<IActionResult> OnGetAsync(string? search = null)
    {
        Search = search;
        var all = await _learningAppService.GetAllCoursesAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            Subjects = all.Where(s => s.Name.ToLower().Contains(term)
                || (s.Description != null && s.Description.ToLower().Contains(term))).ToList();
        }
        else
        {
            Subjects = all;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRegisterAsync(long subjectId)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        try
        {
            await _learningAppService.RegisterSubjectAsync(subjectId);
            TempData["Success"] = "Đăng ký môn học thành công! Bạn có thể bắt đầu học ngay bây giờ.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostUnregisterAsync(long subjectId)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        try
        {
            await _learningAppService.UnregisterSubjectAsync(subjectId);
            TempData["Success"] = "Đã hủy đăng ký môn học.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("./Index");
    }
}
