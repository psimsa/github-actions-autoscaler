# AGENTS.md - Developer Guide for AI Coding Agents

This guide helps AI agents work effectively in this repository. It captures build/test commands, style rules, and architectural conventions.

## Project Overview

This is a .NET 10 ASP.NET Core application that autoscales GitHub Actions runners. It is a single deployable app with a mode switch and pluggable queue/runner providers.

## Build, Test, and Lint Commands

### Build
```bash
# Build the main app
dotnet build src/GithubActionsAutoscaler

# Build entire solution
dotnet build

# Release build
dotnet build -c Release

# Clean artifacts
dotnet clean
```

### Test
```bash
# Run all tests
dotnet test

# Run a single test file
dotnet test --filter "FullyQualifiedName~GithubActionsAutoscaler.Tests.Unit.Services.WorkflowProcessorTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName=GithubActionsAutoscaler.Tests.Unit.Services.WorkflowProcessorTests.ProcessWorkflowAsync_WhenRepoAllowed_StartsRunner"
```

### Run
```bash
dotnet run --project src/GithubActionsAutoscaler
dotnet run --project src/GithubActionsAutoscaler -c Release
```

### Lint/Format
No explicit linting tools are configured. Use `dotnet build` to surface warnings.

### Docker
```bash
docker build -t github-actions-autoscaler .
docker run -p 8080:8080 github-actions-autoscaler
```

## Code Style Guidelines

### General
- Target framework: .NET 10.0, C# 14
- Nullable reference types: enabled
- Implicit usings: enabled

### Formatting
- Indentation: tabs (not spaces)
- Braces: opening brace on same line
- No strict line length, prefer readability

### Imports
- Use implicit usings for standard namespaces
- Explicit `using` for non-standard namespaces
- Order: System, third-party, local

### Naming
- Classes/Interfaces: PascalCase (e.g., `DockerRunnerManager`, `IQueueProvider`)
- Methods: PascalCase, async methods end with `Async`
- Properties: PascalCase
- Fields: `_camelCase`
- Locals/parameters: camelCase

### Types
- Use `record` for immutable DTOs (e.g., Workflow models)
- Use `class` for services and mutable state
- Initialize collections with `[]` (never null)

### Error Handling
- Use structured logging with `ILogger<T>`
- Log errors with `_logger.LogError(ex, "message")`
- Return false/empty results for expected failures

### Dependency Injection
- Constructor injection only
- Inject interfaces, not concrete types

### Configuration
- Options-based config under `Configuration/`
- Defaults are in `appsettings.json`
- `appsettings.custom.json` is optional and not committed

### Telemetry
- Use `Activity` events for operational messages
- Info logs only for lifecycle events
- Metrics: use `AutoscalerMetrics` (Abstractions)

## Architecture Conventions

### Project Layout
```
src/
├── GithubActionsAutoscaler/               # Main app
├── GithubActionsAutoscaler.Abstractions/  # Shared contracts
├── GithubActionsAutoscaler.Queue.Azure/   # Azure queue provider
└── GithubActionsAutoscaler.Runner.Docker/ # Docker runner provider
```

### Key Services
- `IQueueProvider` and `IQueueMessage` for queue interactions
- `IRunnerManager` for runner lifecycle
- `WorkflowProcessor` orchestrates queue → runner workflow
- `QueueMonitorWorker` handles polling
- `RepositoryFilter` / `LabelMatcher` for workflow filtering
- `AutoscalerMetrics` (optional) for OpenTelemetry instrumentation

### Service Registration & OperationMode Flow
Startup in [Program.cs](src/GithubActionsAutoscaler/Program.cs) delegates to [AutoscalerSetupExtensions](src/GithubActionsAutoscaler/Extensions/AutoscalerSetupExtensions.cs). Service registration is **conditionally driven by `OperationMode`**:

- **Webhook mode**: Registers HTTP endpoints only (`MapWorkflowEndpoints`); no workers started
- **QueueMonitor mode**: Registers `QueueMonitorWorker` (IHostedService); continuously polls queue via `IQueueProvider`
- **Both mode**: Registers both endpoints and worker

`AutoscalerMetrics` is **optional** (nullable)—only created if OpenTelemetry is enabled. Services handle null gracefully.

### Integration Flow
1. **Startup**: `AutoscalerSetupExtensions` conditionally registers based on `OperationMode`
2. **Webhook pathway**: HTTP POST → [WorkflowEndpoints](src/GithubActionsAutoscaler/Endpoints/WorkflowEndpoints.cs) → (Base64 decode JSON) → `WorkflowProcessor`
3. **QueueMonitor pathway**: [QueueMonitorWorker](src/GithubActionsAutoscaler/Workers/QueueMonitorWorker.cs) polls `IQueueProvider` → `WorkflowProcessor`
4. **Processing**: `WorkflowProcessor` applies `RepositoryFilter` / `LabelMatcher` → calls `IRunnerManager.CreateRunnerAsync()`
5. **Cleanup**: `QueueMonitorWorker` periodically calls `IRunnerManager.CleanupOldRunnersAsync()` when workflow action is 'completed'

### Message Format & Serialization
- Queue messages are **Base64-encoded JSON** (applied in `WorkflowEndpoints`)
- Decode before deserialization: `Convert.FromBase64String` → JSON parse
- Workflow payload contains repository, labels, and action fields used for filtering

### Retry Logic & Message Tracking
`QueueMonitorWorker` tracks `_lastUnsuccessfulMessageId` to prevent infinite retries:
- Messages that fail to process remain on queue but are skipped for 10 seconds
- Implements exponential backoff indirectly via polling interval
- See [QueueMonitorWorker.cs](src/GithubActionsAutoscaler/Workers/QueueMonitorWorker.cs) for implementation

### Distributed Tracing with Activity
Services use `System.Diagnostics.Activity.Current?.AddEvent()` for non-intrusive tracing:
```csharp
Activity.Current?.AddEvent(new("workflow_processed", tags: new() { 
    { "repo", workflow.Repository }, 
    { "runner_id", runnerInstance.Id } 
}));
```
This integrates with OpenTelemetry without requiring explicit span creation.

## Extending with Custom Providers

### Adding a New Queue Provider
1. Create project `GithubActionsAutoscaler.Queue.{Provider}/`
2. Implement `IQueueProvider` interface (see [IQueueProvider.cs](src/GithubActionsAutoscaler.Abstractions/Queue/IQueueProvider.cs))
3. Implement `IQueueMessage` for message type
4. Create `{Provider}QueueOptions` record inheriting `QueueOptions` base
5. Add `IValidateOptions<{Provider}QueueOptions>` validator
6. Create `ServiceCollectionExtensions.AddQueue{Provider}()` extension method
7. Call extension from `Program.cs` when provider is selected via config

See [AzureQueueProvider](src/GithubActionsAutoscaler.Queue.Azure/) for reference implementation.

### Adding a New Runner Manager
1. Create project `GithubActionsAutoscaler.Runner.{Provider}/`
2. Implement `IRunnerManager` interface (see [IRunnerManager.cs](src/GithubActionsAutoscaler.Abstractions/Runner/IRunnerManager.cs))
3. Create `{Provider}RunnerInstance` record with runner metadata
4. Create `{Provider}RunnerOptions` record with provider-specific config
5. Add `IValidateOptions<{Provider}RunnerOptions>` validator
6. Create `ServiceCollectionExtensions.AddRunner{Provider}()` extension method

See [DockerRunnerManager](src/GithubActionsAutoscaler.Runner.Docker/) for reference implementation.

## Configuration Validation

Configuration uses `IValidateOptions<T>` pattern with cascading validation:
- Each `*Options` class has corresponding validator in `Validation/` subfolder
- Validators throw `OptionsValidationException` with detailed error messages
- Validation occurs during host startup (fail-fast approach)
- See [RepositoryFilterValidator](src/GithubActionsAutoscaler/Configuration/Validation/) for filtering logic example

## IHostedService Lifecycle

`QueueMonitorWorker` implements `IHostedService`:
- **`StartAsync()`**: Initializes resources, starts background polling loop
- **`StopAsync()`**: Gracefully stops polling, cleans up resources
- Registrations in `Program.cs` via `.AddHostedService<QueueMonitorWorker>()`
- DI container manages lifetime; only runs when `OperationMode` includes QueueMonitor

## Tests

- Test project: `tests/GithubActionsAutoscaler.Tests.Unit`
- No FluentAssertions; use xUnit `Assert.*`
- Test naming: `{Method}_{Scenario}_{Expected}` (e.g., `ProcessWorkflowAsync_WhenRepoAllowed_StartsRunner`)
- Mock setup with Moq: use `.Setup()` chains and `.Verify()` for assertions
- Test organization: Mirror source structure (e.g., `Tests.Unit/Services/` mirrors `src/.../Services/`)

## Security Notes

- Never log tokens or secrets (use configured secrets manager)
- Respect mode behavior: Webhook vs QueueMonitor vs Both
- Validate input from queue before processing (prevent injection attacks)
- Repository filters enforce allowlist/denylist access control

## References

- Installation guide: [README.md](README.md)
- Implementation workflow: [docs/IMPLEMENTATION-WORKFLOW.md](docs/IMPLEMENTATION-WORKFLOW.md)
- Phase plans: [docs/PHASE-*-PLAN.md](docs/)
- Related instructions: [.github/instructions/](github/instructions/)
