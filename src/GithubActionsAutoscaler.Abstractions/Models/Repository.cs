using System.Text.Json.Serialization;

namespace GithubActionsAutoscaler.Abstractions.Models;

public record Repository
{
    public Repository(string FullName, string Name)
    {
        this.FullName = FullName;
        this.Name = Name;
    }

    [JsonPropertyName("full_name")]
    public string FullName { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    public void Deconstruct(out string FullName, out string Name)
    {
        FullName = this.FullName;
        Name = this.Name;
    }
}
