using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Dtos;

namespace LearningBff.Pages.Learning;

public class SubjectModel : AbpPageModel
{
    private readonly LearningAppService _learningAppService;
    private readonly ICurrentUser _currentUser;

    public SubjectModel(LearningAppService learningAppService, ICurrentUser currentUser)
    {
        _learningAppService = learningAppService;
        _currentUser = currentUser;
    }

    public SubjectStudyDto? CurrentSubject { get; set; }
    public LessonDetailDto? CurrentLesson { get; set; }
    public long SelectedSubjectId { get; set; }
    public long? SelectedLessonId { get; set; }
    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public async Task<IActionResult> OnGetAsync(long? subjectId = null, long? lessonId = null)
    {
        if (!IsAuthenticated)
        {
            return Redirect("/Account/Login");
        }

        // Bắt buộc phải có subjectId
        if (!subjectId.HasValue)
        {
            return RedirectToPage("/Learning/Index");
        }

        SelectedSubjectId = subjectId.Value;

        try
        {
            CurrentSubject = await _learningAppService.GetCourseStudyAsync(subjectId.Value);
        }
        catch
        {
            // Môn học không tồn tại hoặc không có quyền → quay về dashboard
            return RedirectToPage("/Learning/Index");
        }

        if (lessonId.HasValue)
        {
            SelectedLessonId = lessonId.Value;
            CurrentLesson = await _learningAppService.GetLessonDetailAsync(lessonId.Value);
        }
        else
        {
            // Mặc định chọn bài học đầu tiên chưa hoàn thành
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
