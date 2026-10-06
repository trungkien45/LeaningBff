using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Dtos;
using LearningBff.Entities;

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

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

    public string? Search { get; set; }
    public CourseFilter Filter { get; set; } = CourseFilter.All;

    public async Task<IActionResult> OnGetAsync(
        int page = 1,
        CourseFilter? filter = null,
        string? search = null)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        Search = search;
        Filter = filter ?? CourseFilter.All;
        CurrentPage = Math.Max(1, page);

        var (items, total) = await _learningAppService.GetMyCoursesAsync(
            string.IsNullOrWhiteSpace(search) ? null : search,
            CurrentPage,
            PageSize,
            Filter);
        MyCourses = items;
        TotalCount = total;

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
            // Lấy info môn học để kiểm tra tiến độ (dùng page 1 tìm trong tất cả)
            var (allCourses, _) = await _learningAppService.GetMyCoursesAsync(null, 1, int.MaxValue, CourseFilter.All);
            var course = allCourses.FirstOrDefault(c => c.Id == subjectId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy môn học đã đăng ký.";
                return RedirectToPage("./Index", new { filter = Filter, search = Search });
            }

            if (course.CompletedLessons > 0)
            {
                TempData["Error"] = $"Không thể hủy đăng ký '{course.Name}' vì bạn đã hoàn thành {course.CompletedLessons} bài học.";
                return RedirectToPage("./Index", new { filter = Filter, search = Search });
            }

            await _learningAppService.UnregisterSubjectAsync(subjectId);
            TempData["Success"] = $"Đã hủy đăng ký môn học '{course.Name}'.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("./Index", new { filter = Filter, search = Search });
    }
}
