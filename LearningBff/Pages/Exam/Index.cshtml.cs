using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Exam;

public class IndexModel : AbpPageModel
{
    private readonly ExamAppService _examAppService;
    private readonly ICurrentUser _currentUser;

    public IndexModel(ExamAppService examAppService, ICurrentUser currentUser)
    {
        _examAppService = examAppService;
        _currentUser = currentUser;
    }

    public List<ExamSummaryDto> ActiveExams { get; set; } = new();
    public List<ExamResultDto> MyHistory { get; set; } = new();
    public List<SubjectFilterDto> Subjects { get; set; } = new();
    public long? SelectedSubjectId { get; set; }
    public string ActiveTab { get; set; } = "exams";
    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public async Task<IActionResult> OnGetAsync(long? subjectId = null, string tab = "exams")
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        SelectedSubjectId = subjectId;
        ActiveTab = tab;

        Subjects = await _examAppService.GetPublishedExamSubjectsAsync();
        ActiveExams = await _examAppService.GetPublishedExamsAsync(subjectId);
        MyHistory = await _examAppService.GetMyExamHistoryAsync(subjectId);

        return Page();
    }

    public async Task<IActionResult> OnPostStartExamAsync(long examId)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        try
        {
            var resultId = await _examAppService.StartExamAsync(examId);
            return RedirectToPage("/Exam/Take", new { resultId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Exam/Index");
        }
    }
}
