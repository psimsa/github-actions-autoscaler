# Issue: Inconsistent Naming

## Current Problem
Some method and variable names are not consistent with C# naming conventions.

## Examples
- `_emptyStruct` should be `_emptyStructValue` or similar
- `IsContainerTooOld` could be `HasContainerExpired`

## Recommendation
Follow consistent C# naming conventions throughout the codebase.

## Implementation Steps

1. Rename variables to be more descriptive:
```csharp
private EmptyStruct _emptyStructValue = new EmptyStruct();
```

2. Rename methods to better reflect their purpose:
```csharp
private bool HasContainerExpired(ContainerListResponse container) =>
    container.Created.ToUniversalTime().AddHours(DockerServiceConstants.ContainerMaxAgeHours) < DateTime.UtcNow;
```

3. Update all usages accordingly:
```csharp
foreach (var containerListResponse in containers.Where(HasContainerExpired))
{
    await _client.Containers.StopContainerAsync(
        containerListResponse.ID,
        new ContainerStopParameters() { WaitBeforeKillSeconds = 20 },
        token
    );
}
```

4. Consider renaming other methods for consistency:
- `ProcessWorkflow` → `HandleWorkflowEvent`
- `CheckIfHasAllLabels` → `HasAllRequiredLabels`
- `CheckIfRepoIsWhitelistedOrHasAllowedPrefix` → `IsRepositoryAllowed`

## Benefits
- Improved code readability
- Better understanding of purpose
- Consistent naming patterns
- Easier maintenance
