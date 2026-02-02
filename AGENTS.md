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

### Service Registration
Startup is in `Program.cs` and uses `AutoscalerSetupExtensions`.

## Tests

- Test project: `tests/GithubActionsAutoscaler.Tests.Unit`
- No FluentAssertions; use xUnit `Assert.*`

## Security Notes

- Never log tokens or secrets
- Respect mode behavior: Webhook vs QueueMonitor vs Both

## Cursor/Copilot Rules

- No `.cursor` or Copilot rules found in this repo.
