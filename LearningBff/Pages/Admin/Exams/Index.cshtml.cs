using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Admin.Exams;

[Authorize(Roles = "admin")]
public class IndexModel : PageModel
{
    private const int PageSize = 10;
    private const int QuestionPickerPageSize = 12;
    private readonly ExamAppService    _examService;
    private readonly SubjectAppService _subjectService;
    private readonly QuestionAppService _questionService;

    public List<ExamSummaryDto> Exams         { get; set; } = new();
    public List<SubjectDto>     Subjects      { get; set; } = new();
    public long?                SubjectFilter { get; set; }
    public string?              SuccessMessage { get; set; }
    public string?              ErrorMessage   { get; set; }
    public int                  CurrentPage    { get; set; }
    public int                  TotalPages     { get; set; }
    public long                 TotalCount      { get; set; }

    public IndexModel(ExamAppService examService, SubjectAppService subjectService, QuestionAppService questionService)
    {
        _examService    = examService;
        _subjectService = subjectService;
        _questionService = questionService;
    }

    public async Task OnGetAsync(long? subjectId, int page = 1)
    {
        SubjectFilter  = subjectId;
        Subjects       = await _subjectService.GetListAsync(isActive: true);
        var safePage = Math.Clamp(page, 1, 1_000_000);
        var pageData = await _examService.GetListPageAsync(
            subjectId, (safePage - 1) * PageSize, PageSize);
        TotalCount = pageData.TotalCount;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        CurrentPage = Math.Clamp(safePage, 1, TotalPages);
        if (CurrentPage != safePage)
        {
            pageData = await _examService.GetListPageAsync(
                subjectId, (CurrentPage - 1) * PageSize, PageSize);
        }
        Exams          = pageData.Items.ToList();
        SuccessMessage = TempData["Success"]?.ToString();
        ErrorMessage   = TempData["Error"]?.ToString();
    }

    public async Task<IActionResult> OnGetQuestionPickerAsync(long subjectId, int page = 1)
    {
        var safePage = Math.Clamp(page, 1, 1_000_000);
        var pageData = await _questionService.GetPageAsync(
            subjectId, null, (safePage - 1) * QuestionPickerPageSize, QuestionPickerPageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)pageData.TotalCount / QuestionPickerPageSize));
        var currentPage = Math.Clamp(safePage, 1, totalPages);
        if (currentPage != safePage)
        {
            pageData = await _questionService.GetPageAsync(
                subjectId, null, (currentPage - 1) * QuestionPickerPageSize, QuestionPickerPageSize);
        }

        return new JsonResult(new
        {
            items = pageData.Items,
            totalCount = pageData.TotalCount,
            currentPage,
            totalPages
        });
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string Title, string? Description, long SubjectId,
        int DurationInMinutes, float PassScore, float MaxScore,
        bool IsPublished, bool ShuffleQuestions, bool ShuffleAnswers,
        int? MaxAttempts, DateTime? StartTime, DateTime? EndTime, List<AddExamQuestionDto>? Questions)
    {
        try
        {
            await _examService.CreateAsync(new CreateExamDto
            {
                Title             = Title,
                Description       = Description,
                SubjectId         = SubjectId,
                DurationInMinutes = DurationInMinutes,
                PassScore         = PassScore,
                MaxScore          = MaxScore,
                IsPublished       = IsPublished,
                ShuffleQuestions  = ShuffleQuestions,
                ShuffleAnswers    = ShuffleAnswers,
                MaxAttempts       = MaxAttempts,
                Questions          = Questions ?? new(),
                StartTime         = StartTime?.ToUniversalTime(),
                EndTime           = EndTime?.ToUniversalTime()
            });
            TempData["Success"] = $"Đã tạo đề thi '{Title}'.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        long Id, string Title, string? Description, long SubjectId,
        int DurationInMinutes, float PassScore, float MaxScore,
        bool IsPublished, bool ShuffleQuestions, bool ShuffleAnswers,
        int? MaxAttempts, DateTime? StartTime, DateTime? EndTime, List<AddExamQuestionDto>? Questions)
    {
        try
        {
            await _examService.UpdateAsync(Id, new UpdateExamDto
            {
                Title             = Title,
                Description       = Description,
                SubjectId         = SubjectId,
                DurationInMinutes = DurationInMinutes,
                PassScore         = PassScore,
                MaxScore          = MaxScore,
                IsPublished       = IsPublished,
                ShuffleQuestions  = ShuffleQuestions,
                ShuffleAnswers    = ShuffleAnswers,
                MaxAttempts       = MaxAttempts,
                Questions          = Questions ?? new(),
                StartTime         = StartTime?.ToUniversalTime(),
                EndTime           = EndTime?.ToUniversalTime()
            });
            TempData["Success"] = $"Đã cập nhật đề thi '{Title}'.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long Id)
    {
        try
        {
            await _examService.DeleteAsync(Id);
            TempData["Success"] = "Đã xóa đề thi.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
