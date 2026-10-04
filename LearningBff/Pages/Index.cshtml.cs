using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using LearningBff.Services;
using LearningBff.Services.Dtos;

namespace LearningBff.Pages;

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
    public bool IsAuthenticated => _currentUser.IsAuthenticated;

    public async Task<IActionResult> OnGetAsync()
    {
        if (IsAuthenticated)
        {
            MyCourses = await _learningAppService.GetMyCoursesAsync();
        }

        return Page();
    }
}