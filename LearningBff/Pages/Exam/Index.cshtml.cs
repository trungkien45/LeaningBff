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

    // Pagination – Đề thi
    public int ExamCurrentPage { get; set; } = 1;
    public int ExamPageSize { get; set; } = 9;
    public int ExamTotalCount { get; set; }
    public int ExamTotalPages => ExamPageSize > 0 ? (int)Math.Ceiling((double)ExamTotalCount / ExamPageSize) : 1;

    // Pagination – Lịch sử
    public int HistoryCurrentPage { get; set; } = 1;
    public int HistoryPageSize { get; set; } = 8;
    public int HistoryTotalCount { get; set; }
    public int HistoryTotalPages => HistoryPageSize > 0 ? (int)Math.Ceiling((double)HistoryTotalCount / HistoryPageSize) : 1;

    public async Task<IActionResult> OnGetAsync(
        long? subjectId = null,
        string tab = "exams",
        int examPage = 1,
        int historyPage = 1)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        SelectedSubjectId = subjectId;
        ActiveTab = tab;
        ExamCurrentPage = Math.Max(1, examPage);
        HistoryCurrentPage = Math.Max(1, historyPage);

        Subjects = await _examAppService.GetPublishedExamSubjectsAsync();

        var (exams, examTotal) = await _examAppService.GetPublishedExamsAsync(subjectId, ExamCurrentPage, ExamPageSize);
        ActiveExams = exams;
        ExamTotalCount = examTotal;

        var (history, histTotal) = await _examAppService.GetMyExamHistoryAsync(subjectId, HistoryCurrentPage, HistoryPageSize);
        MyHistory = history;
        HistoryTotalCount = histTotal;

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
