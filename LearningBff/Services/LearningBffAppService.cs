using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using LearningBff.Localization;

namespace LearningBff.Services;

/* Inherit your application services from this class. */
[Authorize]
public abstract class LearningBffAppService : ApplicationService
{
    protected LearningBffAppService()
    {
        LocalizationResource = typeof(LearningBffResource);
    }
}