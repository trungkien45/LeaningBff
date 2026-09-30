using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Subjects;

[Authorize]
public class IndexModel : PageModel
{
    private readonly EnrollmentSubjectAppService _registrationService;

    public List<EnrollmentSubjectDto> Subjects { get; set; } = new();

    public IndexModel(EnrollmentSubjectAppService registrationService)
    {
        _registrationService = registrationService;
    }

    public async Task OnGetAsync()
    {
        Subjects = await _registrationService.GetSubjectsAsync();
    }

    public async Task<IActionResult> OnPostRegisterAsync(long subjectId)
    {
        try
        {
            await _registrationService.RegisterAsync(subjectId);
            TempData["Success"] = "Đăng ký môn học thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnregisterAsync(long subjectId)
    {
        try
        {
            await _registrationService.UnregisterAsync(subjectId);
            TempData["Success"] = "Đã hủy đăng ký môn học.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }
}