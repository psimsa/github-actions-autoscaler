# Issue: Potential Security Issues

## Current Problem
Sensitive information is passed as environment variables.

## Recommendation
Consider using a more secure secrets management approach.

## Implementation Steps

1. Use Azure Key Vault for secrets management:
```csharp
public class KeyVaultSecretProvider : ISecretProvider
{
    private readonly SecretClient _client;

    public KeyVaultSecretProvider(string vaultUrl)
    {
        _client = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        var secret = await _client.GetSecretAsync(secretName);
        return secret.Value.Value;
    }
}
```

2. Update configuration to use secrets provider:
```csharp
public class AppConfiguration
{
    // ... existing properties ...
    public ISecretProvider SecretProvider { get; set; }
}
```

3. Update configuration loading:
```csharp
public static AppConfiguration FromConfiguration(IConfiguration configuration)
{
    var vaultUrl = configuration.GetValue<string>("KeyVaultUrl");
    var secretProvider = string.IsNullOrWhiteSpace(vaultUrl)
        ? null
        : new KeyVaultSecretProvider(vaultUrl);

    return new AppConfiguration
    {
        // ... existing configuration loading ...
        GithubToken = await GetSecretAsync(configuration, secretProvider, "GithubToken"),
        DockerToken = await GetSecretAsync(configuration, secretProvider, "DockerToken"),
        SecretProvider = secretProvider
    };
}

private static async Task<string> GetSecretAsync(
    IConfiguration configuration,
    ISecretProvider secretProvider,
    string secretName)
{
    if (secretProvider != null)
    {
        return await secretProvider.GetSecretAsync(secretName);
    }

    return configuration.GetValue<string>(secretName);
}
```

4. Add secret validation:
```csharp
public class SecretValidator
{
    public void ValidateSecret(string secret, string secretName)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException($"{secretName} secret is required");
        }
    }
}
```

5. Update configuration validator:
```csharp
public class AppConfigurationValidator
{
    private readonly SecretValidator _secretValidator = new SecretValidator();

    public void Validate(AppConfiguration config)
    {
        // ... existing validation ...
        _secretValidator.ValidateSecret(config.GithubToken, "GitHub Token");
        _secretValidator.ValidateSecret(config.DockerToken, "Docker Token");
    }
}
```

6. Use environment-specific configuration:
```csharp
public static AppConfiguration FromConfiguration(IConfiguration configuration)
{
    var environment = configuration.GetValue<string>("EnvironmentName");
    var config = new AppConfiguration
    {
        // ... existing configuration loading ...
        Environment = environment
    };

    // Load environment-specific configuration
    if (!string.IsNullOrWhiteSpace(environment))
    {
        var envConfig = configuration.GetSection(environment);
        if (envConfig != null)
        {
            // Override with environment-specific values
        }
    }

    return config;
}
```

## Benefits
- Better security for sensitive information
- Improved secrets management
- Environment-specific configuration
- Clearer separation of secrets and configuration
- Easier rotation of secrets
