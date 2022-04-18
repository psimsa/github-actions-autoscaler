using System.Text.Json.Serialization;

namespace AutoscalerApi.Controllers;

public record Repository(
    [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("name")] string Name);
