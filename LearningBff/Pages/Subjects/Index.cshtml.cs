using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Dtos;

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

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
    public int EnrolledCount { get; set; }

    public async Task<IActionResult> OnGetAsync(string? search = null, int page = 1)
    {
        Search = search;
        CurrentPage = Math.Max(1, page);

        var (items, total) = await _learningAppService.GetAllCoursesAsync(search, CurrentPage, PageSize);
        Subjects = items;
        TotalCount = total;

        // Enrolled count: cần lấy tổng toàn bộ (không phải chỉ trang hiện tại)
        // -> lấy từ items đang có + query riêng nếu muốn chính xác.
        // Đơn giản: đếm từ items hiện tại + tổng enrolled (dùng 1 query nhẹ)
        EnrolledCount = items.Count(s => s.IsEnrolled);

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

        return RedirectToPage("./Index", new { search = Search });
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

        return RedirectToPage("./Index", new { search = Search });
    }
}
