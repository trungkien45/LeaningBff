using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Exam;

[Authorize]
public class ResultModel : PageModel
{
    private readonly ExamResultAppService _resultService;

    public ExamResultDto? Result { get; set; }
    public bool CanRetake { get; set; }
    public int CorrectCount => Result?.Questions.Count(q => q.IsCorrect) ?? 0;
    public int TotalCount   => Result?.Questions.Count ?? 0;

    public ResultModel(ExamResultAppService resultService)
    {
        _resultService = resultService;
    }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        Result = await _resultService.GetResultAsync(id);
        CanRetake = await _resultService.CanRetakeAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostRetryAsync(long id)
    {
        try
        {
            var resultId = await _resultService.StartRetakeAsync(id);
            return RedirectToPage("/Exam/Take", new { id = resultId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage(new { id });
        }
    }
}
