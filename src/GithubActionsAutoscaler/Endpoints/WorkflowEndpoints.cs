using System.Text.Json.Nodes;
using GithubActionsAutoscaler.Abstractions.Queue;
using Microsoft.AspNetCore.Mvc;

namespace GithubActionsAutoscaler.Endpoints;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("workflow");
        group
            .MapGet("ping", () => Results.Ok(new { message = "Pong" }))
            .WithName("Healthcheck")
            .WithOpenApi();

        group
            .MapPost(
                "enqueue-job",
				async ([FromBody] JsonNode job, IQueueProvider queueProvider) =>
				{
					string messageText = job.ToJsonString();
					string base64MessageText = Convert.ToBase64String(
						System.Text.Encoding.UTF8.GetBytes(messageText)
					);
					await queueProvider.InitializeAsync();
					await queueProvider.SendMessageAsync(base64MessageText);
					return Results.Ok();
				}
			)
            .WithName("Enqueue Job")
            .WithOpenApi();

        return builder;
    }
}
