using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace LearningBff.Pages.Admin;

[Authorize(Roles = "admin")]
public class IndexModel : AbpPageModel
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IIdentityRoleRepository _roleRepository;

    public long TotalUsers { get; set; }
    public long TotalRoles { get; set; }
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string[] AdminRoles { get; set; } = Array.Empty<string>();
    public string OsDescription { get; set; } = string.Empty;
    public string FrameworkDescription { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }

    public IndexModel(
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task OnGetAsync()
    {
        TotalUsers = await _userRepository.GetCountAsync();
        TotalRoles = await _roleRepository.GetCountAsync();

        AdminUserName = CurrentUser.UserName ?? "Admin";
        AdminEmail = CurrentUser.Email ?? "admin@abp.io";
        AdminRoles = CurrentUser.Roles ?? new[] { "admin" };

        OsDescription = RuntimeInformation.OSDescription;
        FrameworkDescription = RuntimeInformation.FrameworkDescription;
        MachineName = Environment.MachineName;
        ServerTime = DateTime.UtcNow;
    }
}
