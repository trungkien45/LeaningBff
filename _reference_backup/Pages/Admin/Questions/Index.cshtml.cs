using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LearningBff.Entities;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Admin.Questions;

[Authorize(Roles = "admin")]
public class IndexModel : PageModel
{
    private const int PageSize = 12;
    private readonly QuestionAppService _questionService;
    private readonly SubjectAppService  _subjectService;

    public List<QuestionDto>  Questions     { get; set; } = new();
    public List<SubjectDto>   Subjects      { get; set; } = new();
    public SubjectDto?        FilterSubject { get; set; }
    public long?              SubjectId     { get; set; }
    public string?            TypeFilter    { get; set; }
    public string?            SuccessMessage { get; set; }
    public string?            ErrorMessage   { get; set; }
    public int                 CurrentPage   { get; set; }
    public int                 TotalPages    { get; set; }
    public long                TotalCount     { get; set; }

    public IndexModel(QuestionAppService questionService, SubjectAppService subjectService)
    {
        _questionService = questionService;
        _subjectService  = subjectService;
    }

    public async Task OnGetAsync(long? subjectId, string? type, int page = 1)
    {
        SubjectId  = subjectId;
        TypeFilter = type;
        Subjects   = await _subjectService.GetListAsync(isActive: true);

        QuestionType? typeEnum = Enum.TryParse<QuestionType>(type, out var parsed) ? parsed : null;
        var safePage = Math.Clamp(page, 1, 1_000_000);
        var pageData = await _questionService.GetPageAsync(
            subjectId, typeEnum, (safePage - 1) * PageSize, PageSize);
        TotalCount = pageData.TotalCount;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        CurrentPage = Math.Clamp(safePage, 1, TotalPages);
        if (CurrentPage != safePage)
        {
            pageData = await _questionService.GetPageAsync(
                subjectId, typeEnum, (CurrentPage - 1) * PageSize, PageSize);
        }
        Questions = pageData.Items.ToList();

        if (subjectId.HasValue)
            FilterSubject = Subjects.FirstOrDefault(s => s.Id == subjectId.Value);

        SuccessMessage = TempData["Success"]?.ToString();
        ErrorMessage   = TempData["Error"]?.ToString();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string Title, QuestionType Type, int Difficulty, float DefaultScore,
        long SubjectId, string? GeneralExplanation,
        List<AnswerInput> Answers)
    {
        try
        {
            var dto = new CreateQuestionDto
            {
                Title              = Title,
                Type               = Type,
                Difficulty         = (Entities.QuestionDifficulty)Difficulty,
                DefaultScore       = DefaultScore,
                SubjectId          = SubjectId,
                GeneralExplanation = GeneralExplanation,
                Answers            = Answers.Select(a => new CreateAnswerDto
                {
                    Text        = a.Text,
                    IsCorrect   = a.IsCorrect,
                    Explanation = a.Explanation,
                    Order       = a.Order
                }).ToList()
            };
            await _questionService.CreateAsync(dto);
            TempData["Success"] = "Đã tạo câu hỏi.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { subjectId = SubjectId });
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        long Id, string Title, QuestionType Type, int Difficulty, float DefaultScore,
        long SubjectId, string? GeneralExplanation,
        List<AnswerInput> Answers)
    {
        try
        {
            var dto = new UpdateQuestionDto
            {
                Title              = Title,
                Type               = Type,
                Difficulty         = (Entities.QuestionDifficulty)Difficulty,
                DefaultScore       = DefaultScore,
                SubjectId          = SubjectId,
                GeneralExplanation = GeneralExplanation,
                Answers            = Answers.Select(a => new CreateAnswerDto
                {
                    Id          = a.Id,
                    Text        = a.Text,
                    IsCorrect   = a.IsCorrect,
                    Explanation = a.Explanation,
                    Order       = a.Order
                }).ToList()
            };
            await _questionService.UpdateAsync(Id, dto);
            TempData["Success"] = "Đã cập nhật câu hỏi.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { subjectId = SubjectId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(long Id)
    {
        try
        {
            await _questionService.DeleteAsync(Id);
            TempData["Success"] = "Đã xóa câu hỏi.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportAsync(long SubjectId, IFormFile File)
    {
        try
        {
            var result = await _questionService.ImportQuestion(new ImportQuestionDto
            {
                SubjectId = SubjectId,
                File = File
            });

            if (result.Errors.Any())
            {
                TempData["Error"] = $"Đã import {result.ImportedQuestions}/{result.TotalQuestions} câu hỏi. Lỗi: {string.Join(" | ", result.Errors)}";
            }
            else
            {
                TempData["Success"] = $"Đã import {result.ImportedQuestions} câu hỏi từ Excel.";
            }
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }

        return RedirectToPage(new { subjectId = SubjectId });
    }

    /// <summary>Model binder helper cho danh sách answers từ form</summary>
    public class AnswerInput
    {
        public long   Id          { get; set; }
        public string Text        { get; set; } = string.Empty;
        public bool   IsCorrect   { get; set; }
        public string? Explanation { get; set; }
        public int    Order       { get; set; }
    }
}
