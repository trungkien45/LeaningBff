using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using LearningBff.Services.Dtos;
using LearningBff.Services;
using LearningBff.Services.Teacher;

namespace LearningBff.Pages.Teacher;

[Authorize(Roles = LearningBffConsts.Teacher)]
public class IndexModel : AbpPageModel
{
    private readonly TeacherDashboardAppService _teacherDashboardAppService;

    public TeacherDashboardDto DashboardData { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IndexModel(TeacherDashboardAppService teacherDashboardAppService)
    {
        _teacherDashboardAppService = teacherDashboardAppService;
    }

    public async Task OnGetAsync(string? search = null, int currentPage = 1)
    {
        Search = search;
        CurrentPage = currentPage < 1 ? 1 : currentPage;
        DashboardData = await _teacherDashboardAppService.GetDashboardAsync(Search, CurrentPage, pageSize: 6);
    }
}
