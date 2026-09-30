using Microsoft.Extensions.Localization;
using LearningBff.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace LearningBff;

[Dependency(ReplaceServices = true)]
public class LearningBffBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<LearningBffResource> _localizer;

    public LearningBffBrandingProvider(IStringLocalizer<LearningBffResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}