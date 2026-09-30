using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LearningBff.HealthChecks;

public static class HealthChecksBuilderExtensions
{
    public static void AddLearningBffHealthChecks(this IServiceCollection services)
    {
        // Add your health checks here
        var healthChecksBuilder = services.AddHealthChecks();
        healthChecksBuilder.AddCheck<LearningBffDatabaseCheck>("LearningBff DbContext Check", tags: new string[] { "database" });

        var configuration = services.GetConfiguration();
        var healthCheckUrl = configuration["App:HealthCheckUrl"];

        if (string.IsNullOrEmpty(healthCheckUrl) || healthCheckUrl.StartsWith("/"))
        {
            var relativePath = string.IsNullOrEmpty(healthCheckUrl) ? "/health-status" : healthCheckUrl;
            var selfUrl = configuration["App:SelfUrl"];
            if (!string.IsNullOrWhiteSpace(selfUrl) &&
                Uri.TryCreate(selfUrl, UriKind.Absolute, out var selfUri) &&
                selfUri.Host != "+" && selfUri.Host != "0.0.0.0" && selfUri.Host != "[::]")
            {
                healthCheckUrl = $"{selfUrl.TrimEnd('/')}{relativePath}";
            }
            else
            {
                healthCheckUrl = $"http://127.0.0.1:8080{relativePath}";
            }
        }
        
        services.ConfigureHealthCheckEndpoint("/health-status");

        var healthChecksUiBuilder = services.AddHealthChecksUI(settings =>
        {
            settings.AddHealthCheckEndpoint("LearningBff Health Status", healthCheckUrl);
        });

        // Set your HealthCheck UI Storage here
        healthChecksUiBuilder.AddInMemoryStorage();

        services.MapHealthChecksUiEndpoints(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-api";
        });
    }

    private static IServiceCollection ConfigureHealthCheckEndpoint(this IServiceCollection services, string path)
    {
        services.Configure<AbpEndpointRouterOptions>(options =>
        {
            options.EndpointConfigureActions.Add(endpointContext =>
            {
                endpointContext.Endpoints.MapHealthChecks(
                    new PathString(path.EnsureStartsWith('/')),
                    new HealthCheckOptions
                    {
                        Predicate = _ => true,
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
                        AllowCachingResponses = false,
                    });
            });
        });

        return services;
    }

    private static IServiceCollection MapHealthChecksUiEndpoints(this IServiceCollection services, Action<global::HealthChecks.UI.Configuration.Options>? setupOption = null)
    {
        services.Configure<AbpEndpointRouterOptions>(routerOptions =>
        {
            routerOptions.EndpointConfigureActions.Add(endpointContext =>
            {
                endpointContext.Endpoints.MapHealthChecksUI(setupOption);
            });
        });

        return services;
    }
}
