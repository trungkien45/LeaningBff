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
public class CurriculumModel : AbpPageModel
{
    private const int MaximumLessonContentLength = 3 * 1024 * 1024;

    private readonly SubjectTeacherAppService _subjectTeacherAppService;
    private readonly ChappterTeacherAppService _chapterTeacherAppService;
    private readonly LessonTeacherAppService _lessonTeacherAppService;

    public CurriculumModel(
        SubjectTeacherAppService subjectTeacherAppService,
        ChappterTeacherAppService chapterTeacherAppService,
        LessonTeacherAppService lessonTeacherAppService)
    {
        _subjectTeacherAppService = subjectTeacherAppService;
        _chapterTeacherAppService = chapterTeacherAppService;
        _lessonTeacherAppService = lessonTeacherAppService;
    }

    [BindProperty(SupportsGet = true)]
    public long? SubjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ContentTooLarge { get; set; }

    public SubjectDto? CurrentSubject { get; set; }
    public List<ChapterDto> Chapters { get; set; } = new();

    public int TotalLessons => Chapters.Sum(c => c.Lessons.Count);

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(long? subjectId = null)
    {
        SubjectId = subjectId;

        if (SubjectId.HasValue && SubjectId.Value > 0)
        {
            try
            {
                CurrentSubject = await _subjectTeacherAppService.GetSubjectDetailAsync(SubjectId.Value);
                Chapters = await _chapterTeacherAppService.GetCurriculumOfSubjectAsync(SubjectId.Value);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
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

    public async Task<IActionResult> OnGetLessonDetailJsonAsync(long lessonId)
    {
        try
        {
            var lesson = await _lessonTeacherAppService.GetLessonDetailAsync(lessonId);
            return new JsonResult(new { success = true, data = lesson });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostCreateChapterAsync(long subjectId, string name, string? description)
    {
        try
        {
            await _chapterTeacherAppService.AddChapterToSubjectAsync(subjectId, new CreateChapterDto
            {
                Name = name,
                Description = description
            });
            SuccessMessage = $"Đã thêm chương '{name.Trim()}' thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId });
    }

    public async Task<IActionResult> OnPostUpdateChapterAsync(long subjectId, long chapterId, string name, string? description)
    {
        try
        {
            await _chapterTeacherAppService.UpdateChapterAsync(subjectId, chapterId, new UpdateChapterDto
            {
                Name = name,
                Description = description
            });
            SuccessMessage = $"Cập nhật chương '{name.Trim()}' thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId });
    }

    public async Task<IActionResult> OnPostDeleteChapterAsync(long subjectId, long chapterId)
    {
        try
        {
            await _chapterTeacherAppService.DeleteChapterAsync(subjectId, chapterId);
            SuccessMessage = "Đã xóa chương học thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId });
    }

    public async Task<IActionResult> OnPostCreateLessonAsync(long subjectId, long chapterId, string title, string? content)
    {
        if (IsLessonContentTooLarge(content))
        {
            return RedirectToPage(new { subjectId });
        }

        try
        {
            await _lessonTeacherAppService.AddLessonToChapterAsync(chapterId, new CreateLessonDto
            {
                Title = title,
                Content = content
            });
            SuccessMessage = $"Đã thêm bài học '{title.Trim()}' thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId });
    }

    public async Task<IActionResult> OnPostUpdateLessonAsync(long subjectId, long lessonId, string title, string? content)
    {
        if (IsLessonContentTooLarge(content))
        {
            return RedirectToPage(new { subjectId });
        }

        try
        {
            await _lessonTeacherAppService.UpdateLessonAsync(lessonId, new UpdateLessonDto
            {
                Title = title,
                Content = content
            });
            SuccessMessage = $"Cập nhật bài học '{title.Trim()}' thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId });
    }

    public async Task<IActionResult> OnPostDeleteLessonAsync(long subjectId, long lessonId)
    {
        try
        {
            await _lessonTeacherAppService.DeleteLessonAsync(lessonId);
            SuccessMessage = "Đã xóa bài học thành công!";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { subjectId });
    }

    private bool IsLessonContentTooLarge(string? content)
    {
        if (content is null || content.Length <= MaximumLessonContentLength)
        {
            return false;
        }

        ErrorMessage = "Nội dung bài học vượt quá giới hạn 3 MB. Ảnh đang nhúng Base64 có thể làm dung lượng tăng nhanh; hãy xóa bớt ảnh hoặc nội dung rồi thử lại.";
        return true;
    }
}
