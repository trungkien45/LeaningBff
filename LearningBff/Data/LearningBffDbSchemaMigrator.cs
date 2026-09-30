using Volo.Abp.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace LearningBff.Data;

public class LearningBffDbSchemaMigrator : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public LearningBffDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<LearningBffDbContext> MigrateAsync()
    {
        /* We intentionally resolving the LearningBffDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        var dbContext = _serviceProvider.GetRequiredService<LearningBffDbContext>();
        await dbContext.Database.MigrateAsync();
        return dbContext;
    }
}
