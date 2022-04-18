using System.Text.Json.Serialization;

namespace AutoscalerApi.Controllers;

public record WorkflowJob(
    [property: JsonPropertyName("labels")] string[] Labels,
    [property: JsonPropertyName("run_id")] string RunId);
