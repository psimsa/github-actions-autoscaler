using System.Text.Json.Serialization;

namespace AutoscalerApi.Models;

public record WorkflowJob
{
    public WorkflowJob(string Name, string[] Labels, long RunId)
    {
        this.Name = Name;
        this.Labels = Labels;
        this.RunId = RunId;
    }

    [JsonPropertyName("name")] public string Name { get; }
    [JsonPropertyName("labels")] public string[] Labels { get; }
    [JsonPropertyName("run_id")] public long RunId { get; }

    public void Deconstruct(out string Name, out string[] Labels, out long RunId)
    {
        Name = this.Name;
        Labels = this.Labels;
        RunId = this.RunId;
    }
}