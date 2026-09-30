using Microsoft.AspNetCore.Identity;
using Volo.Abp.Identity;

namespace LearningBff;

public class DebugAutoLoginMiddleware
{
    private readonly RequestDelegate _next;

    public DebugAutoLoginMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IdentityUserManager userManager,
        SignInManager<Volo.Abp.Identity.IdentityUser> signInManager)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var acceptsHtml = context.Request.Headers.Accept.ToString()
            .Contains("text/html", StringComparison.OrdinalIgnoreCase);

        // Không tự động login khi truy cập trang Login/Logout hoặc static files để không bị vòng lặp xoay
        if (context.Request.Method != HttpMethods.Get ||
            !acceptsHtml ||
            path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/__bundles", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/libs", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
            path.Contains('.'))
        {
            await _next(context);
            return;
        }

        // Nếu chưa đăng nhập, tiến hành tự động login tài khoản admin
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            // "admin" là userName mặc định của ABP, bạn có thể đổi nếu dùng tên khác
            var adminUser = await userManager.FindByNameAsync("admin");

            if (adminUser != null)
            {
                // Thực hiện đăng nhập và tạo Cookie phiên làm việc
                await signInManager.SignInAsync(adminUser, isPersistent: true);

                // Gán lại User vào Context hiện tại để Request này có hiệu lực ngay lập tức
                var claimsPrincipal = await signInManager.CreateUserPrincipalAsync(adminUser);
                context.User = claimsPrincipal;
            }
        }

        await _next(context);
    }
}
