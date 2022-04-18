using System.Text.Json.Serialization;

namespace AutoscalerApi.Controllers;

public record Workflow(string action, [property: JsonPropertyName("workflow_job")]WorkflowJob job, Repository repository);