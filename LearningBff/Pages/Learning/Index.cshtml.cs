using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Learning;

public class IndexModel : AbpPageModel
{
    private readonly LearningAppService _learningAppService;
    private readonly ICurrentUser _currentUser;

    public IndexModel(LearningAppService learningAppService, ICurrentUser currentUser)
    {
        _learningAppService = learningAppService;
        _currentUser = currentUser;
    }

    public List<SubjectCardDto> MyCourses { get; set; } = new();
    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        MyCourses = await _learningAppService.GetMyCoursesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUnregisterAsync(long subjectId)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        try
        {
            // Chỉ cho phép hủy nếu chưa học bài nào
            var courses = await _learningAppService.GetMyCoursesAsync();
            var course = courses.FirstOrDefault(c => c.Id == subjectId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy môn học đã đăng ký.";
                return RedirectToPage("./Index");
            }

            if (course.CompletedLessons > 0)
            {
                TempData["Error"] = $"Không thể hủy đăng ký '{course.Name}' vì bạn đã hoàn thành {course.CompletedLessons} bài học.";
                return RedirectToPage("./Index");
            }

            await _learningAppService.UnregisterSubjectAsync(subjectId);
            TempData["Success"] = $"Đã hủy đăng ký môn học '{course.Name}'.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("./Index");
    }
}
