# Issue: Inconsistent Configuration

## Current Problem
Configuration is accessed in different ways.

## Recommendation
Use a consistent configuration pattern throughout the codebase.

## Implementation Steps

1. Create a configuration provider interface:
```csharp
public interface IConfigurationProvider
{
    T GetValue<T>(string key, T defaultValue = default);
    string GetConnectionString(string name);
    IConfigurationSection GetSection(string key);
}
```

2. Implement a configuration provider:
```csharp
public class ConfigurationProvider : IConfigurationProvider
{
    private readonly IConfiguration _configuration;

    public ConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public T GetValue<T>(string key, T defaultValue = default)
    {
        return _configuration.GetValue(key, defaultValue);
    }

    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name);
    }

    public IConfigurationSection GetSection(string key)
    {
        return _configuration.GetSection(key);
    }
}
```

3. Update services to use the configuration provider:
```csharp
public class DockerService : IDockerService
{
    private readonly IConfigurationProvider _configProvider;

    public DockerService(
        IDockerClientWrapper dockerClient,
        IConfigurationProvider configProvider,
        ILogger<DockerService> logger)
    {
        _dockerClient = dockerClient;
        _configProvider = configProvider;
        _logger = logger;
        
        // Initialize configuration
        InitializeConfiguration();
    }

    private void InitializeConfiguration()
    {
        _dockerImage = _configProvider.GetValue("DockerImage", "myoung34/github-runner:latest");
        _maxRunners = _configProvider.GetValue("MaxRunners", 4);
        // ... other configuration values ...
    }
}
```

4. Update dependency injection:
```csharp
builder.Services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
builder.Services.AddSingleton<IDockerService, DockerService>();
```

5. Create configuration sections:
```csharp
public class DockerConfiguration
{
    public string Image { get; set; } = "myoung34/github-runner:latest";
    public int MaxRunners { get; set; } = 4;
    public string Host { get; set; } = "unix:/var/run/docker.sock";
}

public class AppConfiguration
{
    public DockerConfiguration Docker { get; set; } = new DockerConfiguration();
    public string GithubToken { get; set; } = "";
    public string DockerToken { get; set; } = "";
    // ... other configuration sections ...
}
```

6. Update configuration loading:
```csharp
public static AppConfiguration FromConfiguration(IConfiguration configuration)
{
    var config = new AppConfiguration();
    configuration.Bind(config);
    return config;
}
```

## Benefits
- Consistent configuration access
- Better organization of configuration
- Easier maintenance
- Clearer separation of concerns
- Improved testability
