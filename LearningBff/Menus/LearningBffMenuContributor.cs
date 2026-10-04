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

        // Online Learning menu
        context.Menu.Items.Insert(
            1,
            new ApplicationMenuItem(
                "Learning",
                "Học trực tuyến",
                "~/Learning",
                icon: "fas fa-chalkboard-teacher",
                order: 1
            )
        );

        // Exam menu for all authenticated users
        context.Menu.Items.Insert(
            2,
            new ApplicationMenuItem(
                "Exam",
                "Thi trắc nghiệm",
                "~/Exam",
                icon: "fas fa-file-signature",
                order: 2
            )
        );

        context.Menu.Items.Insert(
            3,
            new ApplicationMenuItem(
                "SubjectRegistration",
                "Đăng ký môn học",
                "~/Subjects",
                icon: "fas fa-book-open",
                order: 3
            )
        );

        var currentUser = context.ServiceProvider.GetRequiredService<ICurrentUser>();
        if (currentUser.IsInRole("teacher") || currentUser.IsInRole("Teacher") || currentUser.IsInRole("admin") || currentUser.IsInRole("Admin"))
        {
            var teacherMenu = new ApplicationMenuItem(
                LearningBffMenus.Teacher,
                l["Menu:Teacher"],
                icon: "fas fa-chalkboard-user",
                order: 4
            );

            teacherMenu.AddItem(new ApplicationMenuItem(
                "Teacher.Dashboard",
                "Dashboard",
                "~/Teacher",
                icon: "fas fa-chart-bar"
            ));

            teacherMenu.AddItem(new ApplicationMenuItem(
                "Teacher.Subjects",
                "Môn học",
                "~/Teacher/Subjects",
                icon: "fas fa-book"
            ));

            teacherMenu.AddItem(new ApplicationMenuItem(
                "Teacher.Questions",
                "Câu hỏi",
                "~/Teacher/Questions",
                icon: "fas fa-question-circle"
            ));

            teacherMenu.AddItem(new ApplicationMenuItem(
                "Teacher.Exams",
                "Đề thi",
                "~/Teacher/Exams",
                icon: "fas fa-file-alt"
            ));

            context.Menu.Items.Insert(3, teacherMenu);
        }

        if (currentUser.IsInRole("admin") || currentUser.IsInRole("Admin"))
        {
            var adminMenu = new ApplicationMenuItem(
                LearningBffMenus.Admin,
                "Quản trị",
                icon: "fas fa-shield-alt",
                order: 5
            );

            adminMenu.AddItem(new ApplicationMenuItem(
                "Admin.Subjects",
                "Quản lý môn học & Giảng viên",
                "~/Admin/Subjects",
                icon: "fas fa-book-bookmark"
            ));

            context.Menu.Items.Add(adminMenu);
        }

        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 6;
        //Administration->Identity
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        //Administration->Settings
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 8);

        return Task.CompletedTask;
    }
}
