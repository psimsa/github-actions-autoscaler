using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;

namespace AutoscalerApi;

public static class EndpointRouteBuilderExtensions
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
                async ([FromBody] JsonNode job, QueueClient queueClient) =>
                {
                    string messageText = job.ToJsonString();
                    string base64MessageText = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes(messageText)
                    );
                    var receipt = await queueClient.SendMessageAsync(base64MessageText);
                    return Results.Ok();
                }
            )
            .WithName("Enqueue Job")
            .WithOpenApi();

        return builder;
    }
}
