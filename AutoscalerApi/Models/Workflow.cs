using System.Text.Json.Serialization;

namespace AutoscalerApi.Controllers;

public record Workflow(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("workflow_job")] WorkflowJob Job,
    [property: JsonPropertyName("repository")] Repository Repository);
