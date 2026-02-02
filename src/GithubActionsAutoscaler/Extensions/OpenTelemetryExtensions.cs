using System.Diagnostics;
using System.Diagnostics.Metrics;
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
		OpenTelemetryOptions options)
	{
		var activitySource = new ActivitySource("GithubActionsAutoscaler");
		var meter = new Meter("GithubActionsAutoscaler");
		services.AddSingleton(activitySource);
		services.AddSingleton(meter);

        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? options.OtlpEndpoint;
        
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName: Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? options.ServiceName)
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
			metrics
				.AddAspNetCoreInstrumentation()
				.AddHttpClientInstrumentation()
				.AddMeter("GithubActionsAutoscaler");
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(e => e.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }
}
