# Issue: Missing Feature Toggles

## Current Problem
Some features are always enabled/disabled based on configuration.

## Recommendation
Consider using feature toggles for better control over functionality.

## Implementation Steps

1. Create a feature toggle configuration:
```csharp
public class FeatureToggles
{
    public bool EnableAutoImageUpdates { get; set; } = true;
    public bool EnableContainerGuard { get; set; } = true;
    public bool EnableQueueMonitoring { get; set; } = true;
    public bool EnableWebEndpoint { get; set; } = false;
}
```

2. Update the main configuration class:
```csharp
public class AppConfiguration
{
    // ... existing properties ...
    public FeatureToggles Features { get; set; } = new FeatureToggles();
}
```

3. Update configuration loading:
```csharp
public static AppConfiguration FromConfiguration(IConfiguration configuration)
{
    return new AppConfiguration
    {
        // ... existing configuration loading ...
        Features = new FeatureToggles
        {
            EnableAutoImageUpdates = configuration.GetValue("Features:EnableAutoImageUpdates", true),
            EnableContainerGuard = configuration.GetValue("Features:EnableContainerGuard", true),
            EnableQueueMonitoring = configuration.GetValue("Features:EnableQueueMonitoring", true),
            EnableWebEndpoint = configuration.GetValue("Features:EnableWebEndpoint", false)
        }
    };
}
```

4. Update feature usage to respect toggles:
```csharp
private async Task<bool> PullImageIfNotExists(CancellationToken token)
{
    if (!_config.Features.EnableAutoImageUpdates)
    {
        _logger.LogInformation("Auto image updates disabled, skipping...");
        return true;
    }
    
    // ... existing implementation ...
}

private async Task ContainerGuard(CancellationToken token)
{
    if (!_config.Features.EnableContainerGuard)
    {
        _logger.LogInformation("Container guard disabled, skipping...");
        return;
    }
    
    // ... existing implementation ...
}
```

5. Add feature toggle validation:
```csharp
public class FeatureToggleValidator
{
    public void Validate(FeatureToggles features)
    {
        if (!features.EnableWebEndpoint && !features.EnableQueueMonitoring)
        {
            throw new ArgumentException("At least one of WebEndpoint or QueueMonitoring must be enabled");
        }
    }
}
```

6. Update the configuration validator:
```csharp
public class AppConfigurationValidator
{
    public void Validate(AppConfiguration config)
    {
        // ... existing validation ...
        
        var featureValidator = new FeatureToggleValidator();
        featureValidator.Validate(config.Features);
    }
}
```

## Benefits
- Better control over feature availability
- Easier feature rollout/rollback
- Improved testing capabilities
- More flexible configuration
