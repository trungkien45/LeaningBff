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
public class SubjectsModel : AbpPageModel
{
    private readonly SubjectTeacherAppService _subjectTeacherAppService;

    public SubjectsModel(SubjectTeacherAppService subjectTeacherAppService)
    {
        _subjectTeacherAppService = subjectTeacherAppService;
    }

    public List<SubjectDto> Subjects { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 8;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public int TotalQuestionsAllSubjects { get; set; }
    public int TotalExamsAllSubjects { get; set; }
    public int TotalStudentsAllSubjects { get; set; }

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

    public async Task OnGetAsync(string? search = null, int currentPage = 1)
    {
        Search = search;
        CurrentPage = currentPage < 1 ? 1 : currentPage;

        var (items, count) = await _subjectTeacherAppService.GetMySubjectsAsync(Search, CurrentPage, PageSize);
        Subjects = items;
        TotalCount = count;
        TotalPages = PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        if (TotalPages > 0 && CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
            var rerun = await _subjectTeacherAppService.GetMySubjectsAsync(Search, CurrentPage, PageSize);
            Subjects = rerun.Items;
        }

        // Aggregate statistics from current page
        TotalQuestionsAllSubjects = Subjects.Sum(s => s.QuestionCount);
        TotalExamsAllSubjects = Subjects.Sum(s => s.ExamCount);
        TotalStudentsAllSubjects = Subjects.Sum(s => s.StudentCount);
    }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostToggleStatusAsync(long subjectId, string? search = null, int currentPage = 1)
    {
        try
        {
            var newStatus = await _subjectTeacherAppService.ToggleSubjectStatusAsync(subjectId);
            SuccessMessage = newStatus
                ? "Đã mở lại đăng ký môn học cho học viên!"
                : "Đã khóa môn học! Học viên sẽ không thể đăng ký mới vào môn học này.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { search, currentPage });
    }
}
