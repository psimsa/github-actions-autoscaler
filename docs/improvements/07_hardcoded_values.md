# Issue: Hardcoded Values

## Current Problem
Some values are hardcoded that should be configurable.

## Examples
- Docker image name `myoung34/github-runner:latest`
- Default labels like `self-hosted`

## Recommendation
Move these to configuration where possible.

## Implementation Steps

1. Update AppConfiguration to include these values:
```csharp
public class AppConfiguration
{
    // ... existing properties
    public string DockerImage { get; set; } = "myoung34/github-runner:latest";
    public string[] DefaultLabels { get; set; } = new[] { "self-hosted" };
}
```

2. Update the configuration loading:
```csharp
public static AppConfiguration FromConfiguration(IConfiguration configuration)
{
    // ... existing code
    return new AppConfiguration
    {
        // ... existing properties
        DockerImage = configuration.GetValue("DockerImage", "myoung34/github-runner:latest"),
        DefaultLabels = configuration.GetValue("DefaultLabels", "self-hosted")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    };
}
```

3. Update DockerService to use these values:
```csharp
public DockerService(
    IDockerClientWrapper dockerClient,
    AppConfiguration configuration,
    ILogger<DockerService> logger)
{
    // ... existing initialization
    _dockerImage = configuration.DockerImage;
    _defaultLabels = configuration.DefaultLabels;
    _labels = configuration.Labels.Concat(_defaultLabels).Distinct().ToArray();
}
```

4. Update the configuration sample in README:
```json
{
  "DockerImage": "myoung34/github-runner:latest",
  "DefaultLabels": "self-hosted,custom-label"
}
```

## Benefits
- Better configurability
- Easier customization
- Improved maintainability
- Clearer separation of concerns
