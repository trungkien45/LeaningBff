using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace LearningBff.Pages.Admin;

[Authorize(Roles = "admin,Admin")]
public class IndexModel : AbpPageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Admin/Subjects/Index");
    }
}
