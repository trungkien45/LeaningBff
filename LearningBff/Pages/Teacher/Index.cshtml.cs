using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using LearningBff.Services;

namespace LearningBff.Pages.Teacher;

[Authorize(Roles = "teacher,Teacher,admin,Admin")]
public class IndexModel : AbpPageModel
{
    private readonly TeacherAppService _teacherAppService;

    public TeacherDashboardDto DashboardData { get; set; } = new();

    public IndexModel(TeacherAppService teacherAppService)
    {
        _teacherAppService = teacherAppService;
    }

    public async Task OnGetAsync()
    {
        DashboardData = await _teacherAppService.GetDashboardAsync();
    }
}
