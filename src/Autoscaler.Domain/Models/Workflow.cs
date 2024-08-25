using System.Text.Json.Serialization;

namespace Autoscaler.Domain.Models;

public record Workflow
{
    public Workflow(string Action, WorkflowJob Job, Repository Repository)
    {
        this.Action = Action;
        this.Job = Job;
        this.Repository = Repository;
    }

    [JsonPropertyName("action")] public string Action { get; }

    [JsonPropertyName("workflow_job")] public WorkflowJob Job { get; }

    [JsonPropertyName("repository")] public Repository Repository { get; }

    public void Deconstruct(out string Action, out WorkflowJob Job, out Repository Repository)
    {
        Action = this.Action;
        Job = this.Job;
        Repository = this.Repository;
    }
}