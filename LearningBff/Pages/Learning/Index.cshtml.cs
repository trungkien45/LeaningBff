using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages.Learning;

public class IndexModel : AbpPageModel
{
    private readonly LearningAppService _learningAppService;
    private readonly ICurrentUser _currentUser;

    public IndexModel(LearningAppService learningAppService, ICurrentUser currentUser)
    {
        _learningAppService = learningAppService;
        _currentUser = currentUser;
    }

    public List<SubjectCardDto> MyCourses { get; set; } = new();
    public SubjectStudyDto? CurrentSubject { get; set; }
    public LessonDetailDto? CurrentLesson { get; set; }
    public long? SelectedSubjectId { get; set; }
    public long? SelectedLessonId { get; set; }
    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public async Task<IActionResult> OnGetAsync(long? subjectId = null, long? lessonId = null)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        MyCourses = await _learningAppService.GetMyCoursesAsync();

        if (subjectId.HasValue)
        {
            SelectedSubjectId = subjectId.Value;
            CurrentSubject = await _learningAppService.GetCourseStudyAsync(subjectId.Value);

            if (lessonId.HasValue)
            {
                SelectedLessonId = lessonId.Value;
                CurrentLesson = await _learningAppService.GetLessonDetailAsync(lessonId.Value);
            }
            else
            {
                // Mặc định chọn bài học đầu tiên chưa hoàn thành, hoặc bài học đầu tiên trong chương đầu tiên
                var firstUncompleted = CurrentSubject.Chapters
                    .SelectMany(c => c.Lessons)
                    .FirstOrDefault(l => !l.IsCompleted);

                var targetLesson = firstUncompleted ?? CurrentSubject.Chapters
                    .SelectMany(c => c.Lessons)
                    .FirstOrDefault();

                if (targetLesson != null)
                {
                    SelectedLessonId = targetLesson.Id;
                    CurrentLesson = await _learningAppService.GetLessonDetailAsync(targetLesson.Id);
                }
            }
        }
        else if (MyCourses.Count == 1)
        {
            // Nếu chỉ có đúng 1 môn đang học, tự động vào môn đó luôn
            return RedirectToPage("/Learning/Index", new { subjectId = MyCourses[0].Id });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostToggleLessonAsync(long lessonId)
    {
        if (!IsAuthenticated)
        {
            return new JsonResult(new { success = false, message = "Chưa đăng nhập" });
        }

        try
        {
            var isCompleted = await _learningAppService.ToggleLessonCompleteAsync(lessonId);
            return new JsonResult(new { success = true, isCompleted });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
}
