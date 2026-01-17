# AGENTS.md - Developer Guide for AI Coding Agents

This guide provides essential information for AI coding agents working in this repository.

## Project Overview

This is a C# .NET 10.0 ASP.NET Core application that autoscales GitHub Actions runners using Docker containers. It monitors Azure Storage Queues for GitHub workflow events and dynamically creates ephemeral runner containers.

## Build, Test, and Lint Commands

### Build Commands
```bash
# Build the main project
dotnet build src/GithubActionsAutoscaler

# Build entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

### Test Commands
**Note:** This project currently has no test suite. Before implementing tests:
1. Check with the user about their preferred testing framework (xUnit, NUnit, MSTest)
2. Create a test project following conventions: `GithubActionsAutoscaler.Tests.Unit`
3. Add test project to solution

### Run Commands
```bash
# Run the application
dotnet run --project src/GithubActionsAutoscaler

# Run with specific configuration
dotnet run --project src/GithubActionsAutoscaler -c Release
```

### Lint/Format Commands
**Note:** No explicit linting tools are configured. The project relies on:
- Built-in C# compiler warnings
- Implicit nullable reference types enforcement (enabled in .csproj)
- Validate with: `dotnet build` (warnings will be displayed)

### Docker Commands
```bash
# Build Docker image (from solution root)
docker build -t github-actions-autoscaler .

# Run container
docker run -p 8080:8080 github-actions-autoscaler
```

## Code Style Guidelines

### General Principles
- **Target Framework:** .NET 10.0
- **Language Version:** C# 14
- **Nullable Reference Types:** Enabled (strict null checking)
- **Implicit Usings:** Enabled

### File Organization
```
src/GithubActionsAutoscaler/
├── Configuration/     # Configuration classes (AppConfiguration)
├── Endpoints/         # API endpoint definitions (WorkflowEndpoints)
├── Models/            # Data models (Workflow, WorkflowJob, Repository)
├── Services/          # Business logic (IDockerService, DockerService)
├── Workers/           # Background services (QueueMonitorWorker)
└── Program.cs         # Application entry point
```

### Naming Conventions
- **Classes/Interfaces:** PascalCase (e.g., `DockerService`, `IDockerService`)
- **Methods:** PascalCase with Async suffix for async methods (e.g., `ProcessWorkflowAsync`, `GetAutoscalerContainersAsync`)
- **Properties:** PascalCase (e.g., `MaxRunners`, `AzureStorage`)
- **Private Fields:** _camelCase with underscore prefix (e.g., `_client`, `_logger`, `_maxRunners`)
- **Local Variables:** camelCase (e.g., `containerName`, `workflowResult`)
- **Constants:** PascalCase (e.g., `EmptyStruct`)
- **Parameters:** camelCase (e.g., `repositoryFullName`, `containerName`)

### Code Formatting
- **Indentation:** Tabs (not spaces)
- **Braces:** Opening brace on same line for methods/classes
- **Line Length:** No strict limit, but prefer readability
- **String Interpolation:** Use `$""` for simple cases, prefer explicit concatenation for complex scenarios

### Type Guidelines
- Always use explicit types for clarity when declaring services/clients
- Use `var` for obvious types (e.g., `var container = new CreateContainerParameters()`)
- Always initialize collections: `[]` for empty arrays (C# 14 collection expressions), never null
- Properties should have explicit types
- Methods must have explicit return types
- Use records for immutable DTOs (see `WorkflowJob`, `Repository`, `Workflow`)

### Imports
- Use implicit usings (enabled by default for .NET 10 web projects)
- Explicit `using` statements only for non-standard namespaces
- Standard order: System namespaces, third-party, local namespaces
- Example from codebase:
```csharp
using GithubActionsAutoscaler.Models;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace GithubActionsAutoscaler.Services;
```

### Async/Await Patterns
- All I/O operations must be async
- Method names: append `Async` suffix (e.g., `ProcessWorkflowAsync`, `GetContainersAsync`)
- Use `Task<T>` for async methods returning values
- Use `Task` for async void equivalents
- Always pass `CancellationToken` where supported
- Example pattern:
```csharp
public async Task<bool> StartContainerAsync(string name, CancellationToken token)
{
    return await _client.Containers.StartContainerAsync(name, new(), token);
}
```

### Dependency Injection
- Constructor injection only (no property/method injection)
- Inject interfaces, not concrete implementations
- Common pattern:
```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly IDockerService _dockerService;
    
    public MyService(ILogger<MyService> logger, IDockerService dockerService)
    {
        _logger = logger;
        _dockerService = dockerService;
    }
}
```

### Error Handling
- Use structured logging with `ILogger<T>`
- Log errors with `_logger.LogError(ex, "message")` 
- Log information with `_logger.LogInformation("message", params)`
- Return `false` or empty results on failure, not exceptions for expected failures
- Example pattern:
```csharp
try
{
    await PerformOperation();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error performing operation");
    return false;
}
```

### Configuration
- Configuration class: `AppConfiguration` in `Configuration/` folder
- Supports both environment variables and JSON files
- Custom config file: `appsettings.custom.json` (not committed)
- Default values should be sensible
- Pattern: Static factory method `FromConfiguration(IConfiguration config)`

### API Endpoints
- Use minimal APIs with `IEndpointRouteBuilder` extensions
- Endpoint definitions in `Endpoints/` folder
- Group related endpoints: `builder.MapGroup("workflow")`
- Use `Results.Ok()`, `Results.BadRequest()`, etc.
- Apply OpenAPI attributes: `.WithName()`, `.WithOpenApi()`

### Comments
- **DO NOT add code comments** unless explicitly requested by the user
- Code should be self-documenting through clear naming
- XML documentation comments are not used in this codebase

### Models and Records
- Use `record` types for immutable data transfer objects
- Use `class` for services and mutable state
- Records should have positional parameters
- Include explicit `Deconstruct` method for records if needed
- Use `[JsonPropertyName]` attributes for JSON mapping

## Important Patterns

### Service Registration (Program.cs)
```csharp
builder.Services.AddSingleton<IDockerService, DockerService>();
builder.Services.AddSingleton(appConfig);
builder.Services.AddHostedService<QueueMonitorWorker>();
```

### Background Workers
- Implement `IHostedService` interface
- Methods: `StartAsync` and `StopAsync`
- Long-running tasks should respect `CancellationToken`

## Azure Services
- Azure Storage Queues for message processing
- Application Insights for telemetry (optional)
- Messages are Base64-encoded JSON

## Docker Integration
- Uses `Docker.DotNet` library
- Default socket: `unix:/var/run/docker.sock`
- Labels pattern: `autoscaler.*` for tracking containers

## Important Notes
- No tests currently exist - consult user before adding test infrastructure
- This is a containerized application - be mindful of Docker host configuration
- Security: Never log tokens or sensitive configuration values
- Environment: Primarily Linux containers, Windows support via Docker host configuration
