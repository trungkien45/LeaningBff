using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using LearningBff.Services.Teacher;
using LearningBff.Dtos;
using LearningBff.Entities;

namespace LearningBff.Pages.Teacher;

[Authorize(Roles = LearningBffConsts.Teacher)]
public class QuestionsModel : AbpPageModel
{
    private readonly SubjectTeacherAppService _subjectTeacherAppService;
    private readonly QuestionBankTeacherAppService _questionBankTeacherAppService;

    public QuestionsModel(
        SubjectTeacherAppService subjectTeacherAppService,
        QuestionBankTeacherAppService questionBankTeacherAppService)
    {
        _subjectTeacherAppService = subjectTeacherAppService;
        _questionBankTeacherAppService = questionBankTeacherAppService;
    }

    [BindProperty(SupportsGet = true)]
    public long? SubjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuestionDifficulty? DifficultyFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuestionType? TypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public SubjectDto? CurrentSubject { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();

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

    public async Task OnGetAsync(
        long? subjectId = null,
        string? search = null,
        QuestionDifficulty? difficultyFilter = null,
        QuestionType? typeFilter = null,
        int currentPage = 1)
    {
        SubjectId = subjectId;
        Search = search;
        DifficultyFilter = difficultyFilter;
        TypeFilter = typeFilter;
        CurrentPage = currentPage < 1 ? 1 : currentPage;

        try
        {
            if (SubjectId.HasValue && SubjectId.Value > 0)
            {
                CurrentSubject = await _subjectTeacherAppService.GetSubjectDetailAsync(SubjectId.Value);
            }

            var (items, count) = await _questionBankTeacherAppService.GetQuestionsOfSubjectAsync(
                SubjectId,
                Search ?? string.Empty,
                CurrentPage,
                PageSize,
                DifficultyFilter,
                TypeFilter
            );

            Questions = items;
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

    public async Task<IActionResult> OnGetQuestionAnswersAsync(long subjectId, long questionId)
    {
        try
        {
            var answers = await _questionBankTeacherAppService.GetAnswersAsync(subjectId, questionId);
            return new JsonResult(new { success = true, data = answers });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostCreateQuestionAsync(
        long subjectId,
        string title,
        QuestionDifficulty difficulty,
        QuestionType type,
        float defaultScore = 1,
        int order = 0,
        string? generalExplanation = null,
        string? search = null,
        int currentPage = 1,
        List<AnswerInput>? Answers = null)
    {
        try
        {
            await _questionBankTeacherAppService.CreateQuestionAsync(subjectId, new CreateQuestionDto
            {
                Title = title,
                Difficulty = difficulty,
                Type = type,
                DefaultScore = defaultScore,
                Order = order,
                GeneralExplanation = generalExplanation ?? string.Empty,
                Answers = (Answers ?? new()).Select(a => new CreateAnswerDto
                {
                    Text = a.Text,
                    IsCorrect = a.IsCorrect,
                    Order = a.Order
                }).ToList()
            });

            SuccessMessage = "Thêm câu hỏi mới thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostUpdateQuestionAsync(
        long subjectId,
        long questionId,
        string title,
        QuestionDifficulty difficulty,
        QuestionType type,
        float defaultScore = 1,
        int order = 0,
        string? generalExplanation = null,
        string? search = null,
        int currentPage = 1,
        List<AnswerInput>? Answers = null)
    {
        try
        {
            await _questionBankTeacherAppService.UpdateQuestionAsync(subjectId, questionId, new UpdateQuestionDto
            {
                Title = title,
                Difficulty = difficulty,
                Type = type,
                DefaultScore = defaultScore,
                Order = order,
                GeneralExplanation = generalExplanation ?? string.Empty,
                Answers = (Answers ?? new()).Select(a => new CreateAnswerDto
                {
                    Text = a.Text,
                    IsCorrect = a.IsCorrect,
                    Order = a.Order
                }).ToList()
            });

            SuccessMessage = "Cập nhật câu hỏi thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostDeleteQuestionAsync(
        long subjectId,
        long questionId,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _questionBankTeacherAppService.DeleteQuestionAsync(subjectId, questionId);
            SuccessMessage = "Đã xóa câu hỏi thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostImportQuestionAsync(
        long subjectId,
        Microsoft.AspNetCore.Http.IFormFile file,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            var result = await _questionBankTeacherAppService.ImportQuestion(new ImportQuestionDto
            {
                SubjectId = subjectId,
                File = file
            });

            SuccessMessage = $"Import hoàn tất: đã nhập {result.ImportedQuestions}/{result.TotalQuestions} câu hỏi.";
            if (result.Errors.Count > 0)
            {
                ErrorMessage = string.Join(Environment.NewLine, result.Errors);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostCreateAnswerAsync(
        long subjectId,
        long questionId,
        string text,
        bool isCorrect,
        int order = 0,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _questionBankTeacherAppService.CreateAnswerAsync(subjectId, questionId, new CreateAnswerDto
            {
                Text = text,
                IsCorrect = isCorrect,
                Order = order
            });
            SuccessMessage = "Thêm đáp án thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostUpdateAnswerAsync(
        long subjectId,
        long questionId,
        long answerId,
        string text,
        bool isCorrect,
        int order = 0,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _questionBankTeacherAppService.UpdateAnswerAsync(subjectId, questionId, answerId, new UpdateAnswerDto
            {
                Text = text,
                IsCorrect = isCorrect,
                Order = order
            });
            SuccessMessage = "Cập nhật đáp án thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    public async Task<IActionResult> OnPostDeleteAnswerAsync(
        long subjectId,
        long questionId,
        long answerId,
        string? search = null,
        int currentPage = 1)
    {
        try
        {
            await _questionBankTeacherAppService.DeleteAnswerAsync(subjectId, questionId, answerId);
            SuccessMessage = "Đã xóa đáp án thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId, search, currentPage });
    }

    /// <summary>Model binder helper for answer list from form</summary>
    public class AnswerInput
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string? Explanation { get; set; }
        public int Order { get; set; }
    }
}
