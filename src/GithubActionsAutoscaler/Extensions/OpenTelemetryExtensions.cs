using System.Diagnostics;
using GithubActionsAutoscaler.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GithubActionsAutoscaler.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryInstrumentation(
        this IServiceCollection services,
        AppConfiguration appConfig)
    {
        var activitySource = new ActivitySource("GithubActionsAutoscaler");
        services.AddSingleton(activitySource);

        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? appConfig.OpenTelemetry.OtlpEndpoint;
        
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName: Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? appConfig.OpenTelemetry.ServiceName)
            )
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource(activitySource.Name);

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithLogging(logging =>
            {
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    logging.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }
}