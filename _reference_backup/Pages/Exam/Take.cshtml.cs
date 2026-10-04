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
public class TakeModel : PageModel
{
    private readonly ExamAppService       _examService;
    private readonly ExamResultAppService _resultService;

    public ExamDto?              Exam             { get; set; }
    public List<ExamQuestionDto> Questions        { get; set; } = new();
    public long                  ResultId         { get; set; }
    public DateTime              StartTime        { get; set; }
    public int                   RemainingSeconds { get; set; }

    public TakeModel(ExamAppService examService, ExamResultAppService resultService)
    {
        _examService   = examService;
        _resultService = resultService;
    }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        // id = ExamResultId
        ResultId = id;
        var result = await _resultService.GetResultAsync(id);

        if (result.Status != Entities.ExamResultStatus.InProgress)
            return RedirectToPage("/Exam/Result", new { id });

        Exam      = await _examService.GetAsync(result.ExamId);
        var startTimeUtc = DateTime.SpecifyKind(result.StartTime, DateTimeKind.Utc);
        StartTime = startTimeUtc;

        var elapsedSeconds = (int)(DateTime.UtcNow - startTimeUtc).TotalSeconds;
        var totalDurationSeconds = (Exam.DurationInMinutes > 0 ? Exam.DurationInMinutes : 45) * 60;
        RemainingSeconds = Math.Max(0, totalDurationSeconds - elapsedSeconds);

        if (RemainingSeconds <= 0)
        {
            // Auto submit empty if time expired
            await _resultService.SubmitExamAsync(new SubmitExamDto
            {
                ExamResultId = id,
                Answers = new List<StudentAnswerDto>()
            });
            return RedirectToPage("/Exam/Result", new { id });
        }

        // Shuffle if needed
        Questions = Exam.ExamQuestions.ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(long ExamResultId, List<StudentAnswerInput> Answers)
    {
        try
        {
            var dto = new SubmitExamDto
            {
                ExamResultId = ExamResultId,
                Answers      = Answers.Select(a => new StudentAnswerDto
                {
                    QuestionId        = a.QuestionId,
                    SelectedAnswerIds = a.SelectedAnswerIds ?? new()
                }).ToList()
            };
            var result = await _resultService.SubmitExamAsync(dto);
            return RedirectToPage("/Exam/Result", new { id = result.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage(new { id = ExamResultId });
        }
    }

    public class StudentAnswerInput
    {
        public long QuestionId { get; set; }
        public List<long>? SelectedAnswerIds { get; set; }
    }
}
