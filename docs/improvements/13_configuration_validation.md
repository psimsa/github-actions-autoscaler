# Issue: Configuration Validation

## Current Problem
Configuration values are not properly validated.

## Recommendation
Add validation to ensure required configuration values are present and valid.

## Implementation Steps

1. Create a configuration validator:
```csharp
public class AppConfigurationValidator
{
    public void Validate(AppConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.GithubToken))
        {
            throw new ArgumentException("GitHub token is required");
        }

        if (config.MaxRunners <= 0)
        {
            throw new ArgumentException("MaxRunners must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(config.DockerImage))
        {
            throw new ArgumentException("DockerImage is required");
        }

        if (config.Labels == null || !config.Labels.Any())
        {
            throw new ArgumentException("At least one label is required");
        }

        if (!string.IsNullOrWhiteSpace(config.AzureStorage) && string.IsNullOrWhiteSpace(config.AzureStorageQueue))
        {
            throw new ArgumentException("AzureStorageQueue is required when AzureStorage is configured");
        }
    }
}
```

2. Update the configuration loading to use validation:
```csharp
public static AppConfiguration FromConfiguration(IConfiguration configuration)
{
    var config = new AppConfiguration
    {
        // ... existing configuration loading ...
    };

    var validator = new AppConfigurationValidator();
    validator.Validate(config);

    return config;
}
```

3. Add validation attributes to the configuration class:
```csharp
public class AppConfiguration
{
    [Required]
    public string GithubToken { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int MaxRunners { get; set; }

    [Required]
    public string DockerImage { get; set; } = "myoung34/github-runner:latest";

    [MinLength(1)]
    public string[] Labels { get; set; } = Array.Empty<string>();

    public string AzureStorage { get; set; } = "";

    [RequiredWhen("AzureStorage", "not empty")]
    public string AzureStorageQueue { get; set; } = "";
}
```

4. Create a custom validation attribute for conditional validation:
```csharp
public class RequiredWhenAttribute : ValidationAttribute
{
    private readonly string _otherProperty;
    private readonly string _otherPropertyValue;

    public RequiredWhenAttribute(string otherProperty, string otherPropertyValue)
    {
        _otherProperty = otherProperty;
        _otherPropertyValue = otherPropertyValue;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var otherProperty = validationContext.ObjectType.GetProperty(_otherProperty);
        if (otherProperty == null)
        {
            return ValidationResult.Success;
        }

        var otherValue = otherProperty.GetValue(validationContext.ObjectInstance)?.ToString();
        if (otherValue == _otherPropertyValue && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
        {
            return new ValidationResult($"{validationContext.DisplayName} is required when {_otherProperty} is {_otherPropertyValue}");
        }

        return ValidationResult.Success;
    }
}
```

5. Add validation error handling:
```csharp
public static AppConfiguration FromConfiguration(IConfiguration configuration)
{
    try
    {
        var config = new AppConfiguration
        {
            // ... existing configuration loading ...
        };

        var validator = new AppConfigurationValidator();
        validator.Validate(config);

        return config;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Invalid configuration", ex);
    }
}
```

## Benefits
- Early detection of configuration issues
- Better error messages for missing/invalid configuration
- Improved system reliability
- Clearer requirements for configuration
