using System.Text.Json.Serialization;

namespace AutoscalerApi.Controllers;

public record WorkflowJob(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("labels")] string[] Labels,
    [property: JsonPropertyName("run_id")] long RunId);
