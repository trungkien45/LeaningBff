using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Exam;

public class ResultModel : AbpPageModel
{
    private readonly ExamAppService _examAppService;
    private readonly ICurrentUser _currentUser;

    public ResultModel(ExamAppService examAppService, ICurrentUser currentUser)
    {
        _examAppService = examAppService;
        _currentUser = currentUser;
    }

    public ExamResultDto ResultData { get; set; } = null!;
    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public async Task<IActionResult> OnGetAsync(long resultId)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        try
        {
            ResultData = await _examAppService.GetResultAsync(resultId);
            return Page();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Exam/Index");
        }
    }

    public async Task<IActionResult> OnPostRetakeAsync(long examId)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        try
        {
            var newResultId = await _examAppService.StartExamAsync(examId);
            return RedirectToPage("/Exam/Take", new { resultId = newResultId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Exam/Index");
        }
    }
}
