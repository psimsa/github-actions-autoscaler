# Issue: Missing Input Validation

## Current Problem
Some methods don't validate their input parameters.

## Recommendation
Add proper input validation to all public methods.

## Implementation Steps

1. Add validation to public methods:
```csharp
public async Task<bool> ProcessWorkflowAsync(Workflow? workflow)
{
    if (workflow == null)
    {
        throw new ArgumentNullException(nameof(workflow));
    }

    if (string.IsNullOrWhiteSpace(workflow.Action))
    {
        throw new ArgumentException("Workflow action cannot be null or empty", nameof(workflow));
    }

    if (workflow.Job == null)
    {
        throw new ArgumentException("Workflow job cannot be null", nameof(workflow));
    }

    if (workflow.Repository == null)
    {
        throw new ArgumentException("Workflow repository cannot be null", nameof(workflow));
    }

    // ... rest of implementation ...
}
```

2. Create validation helpers:
```csharp
public static class ValidationHelpers
{
    public static void ValidateNotNull(object value, string paramName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void ValidateNotEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
        }
    }

    public static void ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be positive");
        }
    }
}
```

3. Use validation helpers in methods:
```csharp
public async Task<bool> StartEphemeralContainerAsync(
    string repositoryFullName,
    string containerName,
    long jobRunId,
    CancellationToken cancellationToken = default)
{
    ValidationHelpers.ValidateNotEmpty(repositoryFullName, nameof(repositoryFullName));
    ValidationHelpers.ValidateNotEmpty(containerName, nameof(containerName));
    ValidationHelpers.ValidatePositive(jobRunId, nameof(jobRunId));

    // ... rest of implementation ...
}
```

4. Add validation to configuration methods:
```csharp
public static AppConfiguration FromConfiguration(IConfiguration configuration)
{
    ValidationHelpers.ValidateNotNull(configuration, nameof(configuration));

    var config = new AppConfiguration
    {
        // ... load configuration ...
    };

    var validator = new AppConfigurationValidator();
    validator.Validate(config);

    return config;
}
```

5. Create a validation attribute for model validation:
```csharp
public class ValidateObjectAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var results = new List<ValidationResult>();
        var context = new ValidationContext(value, null, null);

        if (!Validator.TryValidateObject(value, context, results, true))
        {
            return new ValidationResult("Validation failed for object", results);
        }

        return ValidationResult.Success;
    }
}
```

## Benefits
- Early detection of invalid inputs
- Better error messages
- Improved system reliability
- Clearer method contracts
- Easier debugging
