using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Exam;

[Authorize]
public class IndexModel : PageModel
{
    private const int PageSize = 8;
    private readonly ExamAppService       _examService;
    private readonly ExamResultAppService _resultService;

    public List<ExamSummaryDto>  Exams     { get; set; } = new();
    public List<ExamResultDto>   FilteredResults { get; set; } = new();
    public List<SubjectFilterDto> ResultSubjects { get; set; } = new();
    public List<ExamSummaryDto> FilteredExams { get; set; } = new();
    public List<SubjectFilterDto> ExamSubjects { get; set; } = new();
    public Dictionary<long, int> AttemptCounts { get; set; } = new();
    public long? SelectedSubjectId { get; set; }
    public long? SelectedExamSubjectId { get; set; }
    public int ResultPage { get; set; }
    public int ResultPageCount { get; set; }
    public int FilteredResultCount { get; set; }
    public int MyResultCount { get; set; }
    public int ExamPage { get; set; }
    public int ExamPageCount { get; set; }
    public int FilteredExamCount { get; set; }

    public IndexModel(ExamAppService examService, ExamResultAppService resultService)
    {
        _examService   = examService;
        _resultService = resultService;
    }

    public async Task OnGetAsync(
        long? resultSubjectId,
        int resultPage = 1,
        long? examSubjectId = null,
        int examPage = 1)
    {
        SelectedSubjectId = resultSubjectId;
        ResultSubjects = await _resultService.GetMyResultSubjectsAsync();
        var safeResultPage = Math.Clamp(resultPage, 1, 1_000_000);
        var resultPageData = await _resultService.GetMyResultsPageAsync(
            resultSubjectId, (safeResultPage - 1) * PageSize, PageSize);
        FilteredResultCount = (int)resultPageData.TotalCount;
        MyResultCount = resultSubjectId.HasValue
            ? await _resultService.GetMyResultsCountAsync()
            : (int)resultPageData.TotalCount;
        ResultPageCount = Math.Max(1, (int)Math.Ceiling((double)FilteredResultCount / PageSize));
        ResultPage = Math.Clamp(safeResultPage, 1, ResultPageCount);
        if (ResultPage != safeResultPage)
        {
            resultPageData = await _resultService.GetMyResultsPageAsync(
                resultSubjectId, (ResultPage - 1) * PageSize, PageSize);
        }
        FilteredResults = resultPageData.Items.ToList();

        SelectedExamSubjectId = examSubjectId;
        ExamSubjects = await _examService.GetPublishedExamSubjectsAsync();
        var safeExamPage = Math.Clamp(examPage, 1, 1_000_000);
        var examPageData = await _examService.GetPublishedExamsPageAsync(
            examSubjectId, (safeExamPage - 1) * PageSize, PageSize);
        FilteredExamCount = (int)examPageData.TotalCount;
        ExamPageCount = Math.Max(1, (int)Math.Ceiling((double)FilteredExamCount / PageSize));
        ExamPage = Math.Clamp(safeExamPage, 1, ExamPageCount);
        if (ExamPage != safeExamPage)
        {
            examPageData = await _examService.GetPublishedExamsPageAsync(
                examSubjectId, (ExamPage - 1) * PageSize, PageSize);
        }
        Exams = FilteredExams = examPageData.Items.ToList();
        AttemptCounts = await _resultService.GetMyAttemptCountsAsync(Exams.Select(exam => exam.Id).ToList());
    }

    public async Task<IActionResult> OnPostStartAsync(long examId)
    {
        try
        {
            var resultId = await _resultService.StartExamAsync(examId);
            return RedirectToPage("/Exam/Take", new { id = resultId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage();
        }
    }
}
