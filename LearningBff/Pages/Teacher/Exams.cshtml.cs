using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using LearningBff.Services.Teacher;
using LearningBff.Dtos;

namespace LearningBff.Pages.Teacher;

[Authorize(Roles = LearningBffConsts.Teacher)]
public class ExamsModel : AbpPageModel
{
    private readonly SubjectTeacherAppService _subjectTeacherAppService;
    private readonly ExamTeacherAppService _examTeacherAppService;
    private readonly QuestionBankTeacherAppService _questionBankTeacherAppService;

    public ExamsModel(
        SubjectTeacherAppService subjectTeacherAppService,
        ExamTeacherAppService examTeacherAppService,
        QuestionBankTeacherAppService questionBankTeacherAppService)
    {
        _subjectTeacherAppService = subjectTeacherAppService;
        _examTeacherAppService = examTeacherAppService;
        _questionBankTeacherAppService = questionBankTeacherAppService;
    }

    [BindProperty(SupportsGet = true)]
    public long? SubjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 8;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public SubjectDto? CurrentSubject { get; set; }
    public List<ExamDto> Exams { get; set; } = new();

    public int StartIndex => TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int EndIndex => Math.Min(CurrentPage * PageSize, TotalCount);

    public int StartPage
    {
        get
        {
            const int maxDisplay = 5;
            int start = Math.Max(1, CurrentPage - maxDisplay / 2);
            int end = Math.Min(TotalPages, start + maxDisplay - 1);
            if (end - start + 1 < maxDisplay)
            {
                start = Math.Max(1, end - maxDisplay + 1);
            }
            return start;
        }
    }

    public int EndPage
    {
        get
        {
            const int maxDisplay = 5;
            int start = Math.Max(1, CurrentPage - maxDisplay / 2);
            int end = Math.Min(TotalPages, start + maxDisplay - 1);
            if (end - start + 1 < maxDisplay)
            {
                end = Math.Min(TotalPages, start + maxDisplay - 1);
            }
            return end;
        }
    }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(long? subjectId = null, string? search = null, int currentPage = 1)
    {
        SubjectId = subjectId;
        Search = search;
        CurrentPage = currentPage < 1 ? 1 : currentPage;

        try
        {
            if (SubjectId.HasValue && SubjectId.Value > 0)
            {
                CurrentSubject = await _subjectTeacherAppService.GetSubjectDetailAsync(SubjectId.Value);
            }

            var (items, count) = await _examTeacherAppService.GetExamsOfSubjectAsync(
                SubjectId,
                Search ?? string.Empty,
                CurrentPage,
                PageSize
            );

            Exams = items;
            TotalCount = count;
            TotalPages = PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task<IActionResult> OnGetSubjectsJsonAsync(string? name = null, int page = 1, int pageSize = 10)
    {
        try
        {
            var (items, totalCount) = await _subjectTeacherAppService.GetMySubjectsAsync(name, page, pageSize);
            return new JsonResult(new
            {
                items = items.Select(s => new { id = s.Id, text = s.Name }),
                totalCount
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { items = new object[0], totalCount = 0, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnGetExamDetailsJsonAsync(long subjectId, long examId)
    {
        try
        {
            var exam = await _examTeacherAppService.GetExamDetailAsync(subjectId, examId);
            return new JsonResult(new { success = true, data = exam });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnGetExamQuestionsJsonAsync(long subjectId, long examId, int page = 1, int pageSize = 10, string? search = null)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var examDetail = await _examTeacherAppService.GetExamWithQuestionsAsync(subjectId, examId);
            var (availableQuestions, totalCount) = await _questionBankTeacherAppService.GetAvailableQuestionsForExamAsync(
                subjectId,
                examId,
                search ?? string.Empty,
                page,
                pageSize);

            return new JsonResult(new
            {
                success = true,
                examQuestions = examDetail.Questions,
                availableQuestions,
                totalCount,
                currentPage = page,
                pageSize,
                maxScore = examDetail.MaxScore
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnGetExamResultsJsonAsync(long subjectId, long examId)
    {
        try
        {
            var (results, totalCount) = await _examTeacherAppService.GetExamResultsOfExamAsync(subjectId, examId, pageSize: 50);
            return new JsonResult(new { success = true, data = results, totalCount });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostCreateExamAsync(
        long subjectId,
        string title,
        string? description,
        int durationInMinutes,
        float passScore,
        float maxScore,
        bool isPublished = false,
        bool shuffleQuestions = false,
        bool shuffleAnswers = false,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? maxAttempts = null,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _examTeacherAppService.CreateExamAsync(subjectId, new CreateExamDto
            {
                Title = title,
                Description = description ?? string.Empty,
                DurationInMinutes = durationInMinutes,
                PassScore = passScore,
                MaxScore = maxScore,
                IsPublished = isPublished,
                ShuffleQuestions = shuffleQuestions,
                ShuffleAnswers = shuffleAnswers,
                StartTime = startTime.HasValue ? DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc) : null,
                EndTime = endTime.HasValue ? DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc) : null,
                MaxAttempts = maxAttempts
            });

            SuccessMessage = $"Đã tạo đề thi '{title.Trim()}' thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostUpdateExamAsync(
        long subjectId,
        long examId,
        string title,
        string? description,
        int durationInMinutes,
        float passScore,
        float maxScore,
        bool isPublished = false,
        bool shuffleQuestions = false,
        bool shuffleAnswers = false,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? maxAttempts = null,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _examTeacherAppService.UpdateExamAsync(subjectId, examId, new UpdateExamDto
            {
                Title = title,
                Description = description ?? string.Empty,
                DurationInMinutes = durationInMinutes,
                PassScore = passScore,
                MaxScore = maxScore,
                IsPublished = isPublished,
                ShuffleQuestions = shuffleQuestions,
                ShuffleAnswers = shuffleAnswers,
                StartTime = startTime.HasValue ? DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc) : null,
                EndTime = endTime.HasValue ? DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc) : null,
                MaxAttempts = maxAttempts
            });

            SuccessMessage = $"Cập nhật đề thi '{title.Trim()}' thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostPublishExamAsync(long subjectId, long examId, string? search = null, int currentPage = 1)
    {
        try
        {
            await _examTeacherAppService.PublishExamAsync(subjectId, examId);
            SuccessMessage = "Đã xuất bản đề thi! Học viên hiện có thể làm bài.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostUnpublishExamAsync(
        long subjectId,
        long examId,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _examTeacherAppService.UnpublishExamAsync(subjectId, examId);
            SuccessMessage = "Đã gỡ xuất bản đề thi và xóa các lượt làm bài (nếu có).";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostDeleteExamAsync(long subjectId, long examId, string? search = null, int currentPage = 1)
    {
        try
        {
            await _examTeacherAppService.DeleteExamAsync(subjectId, examId);
            SuccessMessage = "Đã xóa đề thi thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostAddQuestionAsync(long subjectId, long examId, long questionId, float score)
    {
        try
        {
            var scoreSummary = await _examTeacherAppService.AddQuestionToExamAsync(subjectId, examId, questionId, score);
            return new JsonResult(new { success = true, scoreSummary });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostUpdateQuestionScoreAsync(long subjectId, long examId, long questionId, float score)
    {
        try
        {
            var scoreSummary = await _examTeacherAppService.UpdateExamQuestionScoreAsync(subjectId, examId, questionId, score);
            return new JsonResult(new { success = true, scoreSummary });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostRemoveQuestionAsync(long subjectId, long examId, long questionId)
    {
        try
        {
            var scoreSummary = await _examTeacherAppService.RemoveQuestionFromExamAsync(subjectId, examId, questionId);
            return new JsonResult(new { success = true, scoreSummary });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
}
