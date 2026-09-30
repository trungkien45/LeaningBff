using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearningBff.Data;

public class LearningBffDbContextFactory : IDesignTimeDbContextFactory<LearningBffDbContext>
{
    public LearningBffDbContext CreateDbContext(string[] args)
    {
        LearningBffGlobalFeatureConfigurator.Configure();
        LearningBffModuleExtensionConfigurator.Configure();

        // https://www.npgsql.org/efcore/release-notes/6.0.html#opting-out-of-the-new-timestamp-mapping-logic
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        LearningBffEfCoreEntityExtensionMappings.Configure();
        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<LearningBffDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Default"));

        return new LearningBffDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}