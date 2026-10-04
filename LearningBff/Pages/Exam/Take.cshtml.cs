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

public class TakeModel : AbpPageModel
{
    private readonly ExamAppService _examAppService;
    private readonly ICurrentUser _currentUser;

    public TakeModel(ExamAppService examAppService, ICurrentUser currentUser)
    {
        _examAppService = examAppService;
        _currentUser = currentUser;
    }

    public ExamTakeDto ExamData { get; set; } = null!;
    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public async Task<IActionResult> OnGetAsync(long resultId)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        try
        {
            ExamData = await _examAppService.GetForTakeAsync(resultId);
            return Page();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Exam/Index");
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(long examResultId, string? answersJson)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        try
        {
            var submitDto = new SubmitExamDto
            {
                ExamResultId = examResultId,
                Answers = new List<StudentAnswerDto>()
            };

            if (!string.IsNullOrWhiteSpace(answersJson))
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<long>>>(answersJson);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        if (long.TryParse(kvp.Key, out var qId))
                        {
                            submitDto.Answers.Add(new StudentAnswerDto
                            {
                                QuestionId = qId,
                                SelectedAnswerIds = kvp.Value
                            });
                        }
                    }
                }
            }

            var result = await _examAppService.SubmitExamAsync(submitDto);
            return RedirectToPage("/Exam/Result", new { resultId = result.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Exam/Index");
        }
    }
}
