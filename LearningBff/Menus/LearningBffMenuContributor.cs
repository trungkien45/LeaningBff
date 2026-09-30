using Microsoft.Extensions.DependencyInjection;
using LearningBff.Permissions;
using LearningBff.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Users;

namespace LearningBff.Menus;

public class LearningBffMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<LearningBffResource>();
        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                LearningBffMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fas fa-home",
                order: 0
            )
        );

        // Exam menu for all authenticated users
        context.Menu.Items.Insert(
            1,
            new ApplicationMenuItem(
                "Exam",
                "Thi trắc nghiệm",
                "~/Exam",
                icon: "fas fa-graduation-cap",
                order: 1
            )
        );

        context.Menu.Items.Insert(
            2,
            new ApplicationMenuItem(
                "SubjectRegistration",
                "Đăng ký môn học",
                "~/Subjects",
                icon: "fas fa-book-open",
                order: 2
            )
        );

        var currentUser = context.ServiceProvider.GetRequiredService<ICurrentUser>();
        if (currentUser.IsInRole("admin"))
        {
            var adminMenu = new ApplicationMenuItem(
                LearningBffMenus.Admin,
                l["Menu:Admin"],
                icon: "fas fa-shield-halved",
                order: 3
            );

            adminMenu.AddItem(new ApplicationMenuItem(
                "Admin.Dashboard",
                "Dashboard",
                "~/Admin",
                icon: "fas fa-chart-bar"
            ));

            adminMenu.AddItem(new ApplicationMenuItem(
                "Admin.Subjects",
                "Môn học",
                "~/Admin/Subjects",
                icon: "fas fa-book"
            ));

            adminMenu.AddItem(new ApplicationMenuItem(
                "Admin.Questions",
                "Câu hỏi",
                "~/Admin/Questions",
                icon: "fas fa-question-circle"
            ));

            adminMenu.AddItem(new ApplicationMenuItem(
                "Admin.Exams",
                "Đề thi",
                "~/Admin/Exams",
                icon: "fas fa-file-alt"
            ));

            context.Menu.Items.Insert(3, adminMenu);
        }

        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 5;
        //Administration->Identity
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        //Administration->Settings
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 8);

        return Task.CompletedTask;
    }
}
