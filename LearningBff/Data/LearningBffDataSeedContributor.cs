using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace LearningBff.Data;

/// <summary>
/// Seeds the system "teacher" role on first run and after migrations.
/// </summary>
public class LearningBffDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IdentityRoleManager _roleManager;
    private readonly IGuidGenerator _guidGenerator;

    public const string TeacherRoleName = "teacher";

    public LearningBffDataSeedContributor(
        IdentityRoleManager roleManager,
        IGuidGenerator guidGenerator)
    {
        _roleManager = roleManager;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _roleManager.FindByNameAsync(TeacherRoleName) == null)
        {
            var role = new IdentityRole(
                _guidGenerator.Create(),
                TeacherRoleName
            )
            {
                IsStatic = true,   // ABP: không cho xóa role static
                IsDefault = false,
                IsPublic = true
            };

            await _roleManager.CreateAsync(role);
        }
    }
}
