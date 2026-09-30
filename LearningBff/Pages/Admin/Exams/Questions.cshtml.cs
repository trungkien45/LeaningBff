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
public class QuestionsModel : PageModel
{
    private const int PageSize = 12;
    private readonly ExamAppService     _examService;
    private readonly QuestionAppService _questionService;
    private readonly SubjectAppService  _subjectService;

    public long              ExamId            { get; set; }
    public ExamDto?          Exam              { get; set; }
    public List<ExamQuestionDto>  ExamQuestions     { get; set; } = new();
    public List<QuestionDto>      AvailableQuestions { get; set; } = new();
    public List<SubjectDto>       Subjects          { get; set; } = new();
    public long?                  BankSubjectId     { get; set; }
    public int                    CurrentPage       { get; set; }
    public int                    TotalPages        { get; set; }
    public long                   TotalCount        { get; set; }
    public string?                SuccessMessage    { get; set; }
    public string?                ErrorMessage      { get; set; }

    public QuestionsModel(ExamAppService examService, QuestionAppService questionService, SubjectAppService subjectService)
    {
        _examService     = examService;
        _questionService = questionService;
        _subjectService  = subjectService;
    }

    public async Task OnGetAsync(long examId, long? subjectId = null, int page = 1)
    {
        ExamId = examId;
        await LoadAsync(subjectId, page);
    }

    private async Task LoadAsync(long? subjectId, int page)
    {
        Exam               = await _examService.GetAsync(ExamId);
        ExamQuestions      = Exam.ExamQuestions;
        Subjects           = await _subjectService.GetListAsync(isActive: true);
        BankSubjectId = subjectId ?? (Request.Query.ContainsKey("subjectId") ? null : Exam.SubjectId);

        var safePage = Math.Clamp(page, 1, 1_000_000);
        var pageData = await _questionService.GetPageAsync(
            BankSubjectId, null, (safePage - 1) * PageSize, PageSize);
        TotalCount = pageData.TotalCount;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        CurrentPage = Math.Clamp(safePage, 1, TotalPages);
        if (CurrentPage != safePage)
        {
            pageData = await _questionService.GetPageAsync(
                BankSubjectId, null, (CurrentPage - 1) * PageSize, PageSize);
        }
        AvailableQuestions = pageData.Items.ToList();
        SuccessMessage     = TempData["Success"]?.ToString();
        ErrorMessage       = TempData["Error"]?.ToString();
    }

    public async Task<IActionResult> OnPostAddQuestionAsync(long examId, long questionId, float score)
    {
        ExamId = examId;
        try
        {
            await _examService.AddQuestionAsync(examId, new AddExamQuestionDto
            {
                QuestionId = questionId,
                Score      = score
            });
            TempData["Success"] = "Đã thêm câu hỏi vào đề thi.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { examId });
    }

    public async Task<IActionResult> OnPostRemoveQuestionAsync(long examId, long questionId)
    {
        ExamId = examId;
        try
        {
            await _examService.RemoveQuestionAsync(examId, questionId);
            TempData["Success"] = "Đã xóa câu hỏi khỏi đề thi.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { examId });
    }
}
