# Re-Analysis - Improvements, Componentization, and Testability

## Date: January 17, 2026

## Current State Summary

After the initial restructuring to .NET 10 and proper folder structure, the project builds successfully with 10 nullable reference warnings. The code is functional but has several areas that can be improved for better testability, maintainability, and component separation.

## Issues Identified

### 1. DockerService is Too Large (314 lines)
The `DockerService` class handles multiple concerns:
- Container lifecycle management
- Repository filtering (allowlist/denylist)
- Label matching
- Image pulling
- Container guard (cleanup)

**Recommendation:** Extract into smaller, focused services:
- `IContainerManager` - Container creation, start, stop, remove
- `IRepositoryFilter` - Allowlist/denylist logic
- `IImageManager` - Image pulling and updates
- `IRunnerLabelMatcher` - Label matching logic

### 2. Configuration Class Issues
The `AppConfiguration` class has several problems:
- Non-nullable `DockerImage` property without initialization
- Null reference warnings when reading configuration values
- Uses `GetValue<string>()` which returns null for missing keys

**Recommendation:**
- Use C# 14 required modifiers
- Add proper null handling with null-coalescing operators
- Consider using options pattern with `IOptions<T>`

### 3. Lack of Abstraction for External Dependencies
Direct dependencies on:
- `QueueClient` (Azure Storage)
- `DockerClient` (Docker.DotNet)

**Recommendation:** Create abstraction interfaces:
- `IQueueService` - Abstract Azure Queue operations
- Keep `IDockerService` but split into smaller interfaces

### 4. QueueMonitorWorker Improvements Needed
- Creates `QueueClient` internally instead of using DI
- Mixes orchestration with message processing
- Error handling deletes messages on any exception

**Recommendation:**
- Inject `QueueClient` or `IQueueService`
- Extract message processing logic
- Improve error handling with retry policies

### 5. Missing Cancellation Token Propagation
Several methods don't properly propagate cancellation tokens:
- `ContainerGuardAsync` uses `CancellationToken.None` internally
- `PullImageIfNotExistsAsync` uses `CancellationToken.None` for image creation

**Recommendation:** Properly propagate cancellation tokens throughout

### 6. Hardcoded Values
Multiple hardcoded values scattered throughout:
- `20_000` ms timeout for container operations
- `3_000` ms delay in `WaitForAvailableRunnerAsync`
- `10_000` ms delays in queue monitoring
- 1 hour container age limit

**Recommendation:** Move to configuration or constants class

### 7. No Input Validation
- API endpoints don't validate input
- Configuration values not validated on startup

**Recommendation:** Add validation middleware and startup validation

## Testability Improvements

### Current Blockers to Testing

1. **Tight Coupling to Docker Client**
   - `DockerClient` is injected directly
   - No interface wrapper for Docker operations
   - Hard to mock container responses

2. **Static Configuration Loading**
   - `AppConfiguration.FromConfiguration()` is static
   - Configuration parsing not testable in isolation

3. **Background Worker Issues**
   - Creates its own `QueueClient`
   - Long-running loop hard to test
   - No way to control timing in tests

4. **Lack of Seams**
   - Methods are mostly private
   - Internal logic not exposed for testing

### Recommended Test Architecture

```
tests/
├── GithubActionsAutoscaler.Tests.Unit/
│   ├── Configuration/
│   │   └── AppConfigurationTests.cs
│   ├── Services/
│   │   ├── DockerServiceTests.cs
│   │   ├── RepositoryFilterTests.cs
│   │   └── LabelMatcherTests.cs
│   └── Workers/
│       └── QueueMonitorWorkerTests.cs
└── GithubActionsAutoscaler.Tests.Integration/
    ├── Api/
    │   └── WorkflowEndpointsTests.cs
    └── Docker/
        └── ContainerManagementTests.cs
```

## Refactoring Plan

### Phase 1: Extract Repository Filtering (Easy Win)
1. Create `IRepositoryFilter` interface
2. Extract `CheckIfRepoIsAllowedOrHasAllowedPrefix` to new class
3. Create unit tests for filtering logic
4. Inject `IRepositoryFilter` into `DockerService`

### Phase 2: Fix Configuration Issues
1. Add proper null handling in `AppConfiguration`
2. Add startup validation
3. Create tests for configuration parsing

### Phase 3: Extract Label Matching
1. Create `ILabelMatcher` interface
2. Move label matching logic to dedicated service
3. Add unit tests

### Phase 4: Improve Queue Worker
1. Inject `QueueClient` via DI
2. Extract message processing to separate method
3. Add better error handling
4. Create integration tests

### Phase 5: Split DockerService
1. Create `IContainerManager` for container operations
2. Create `IImageManager` for image operations
3. Keep `DockerService` as orchestrator
4. Add comprehensive tests

### Phase 6: Add Constants and Configuration
1. Create `AutoscalerConstants` class
2. Move timeouts to configuration
3. Add configuration validation

## Recommended Testing Framework

Based on modern .NET practices:
- **Unit Tests:** xUnit + Moq + FluentAssertions
- **Integration Tests:** xUnit + Testcontainers (for Docker tests)
- **Coverage:** Coverlet for code coverage

## Priority Order for Implementation

1. Fix nullable warnings (quick fix, improves code quality)
2. Extract `IRepositoryFilter` (easy, high testability impact)
3. Create unit test project with first tests
4. Extract `ILabelMatcher`
5. Improve configuration handling
6. Refactor `QueueMonitorWorker`
7. Split `DockerService`
8. Add integration tests

## Estimated Effort

- Phase 1 (Repository Filter): 1-2 hours
- Phase 2 (Configuration): 1 hour
- Phase 3 (Label Matcher): 1 hour
- Phase 4 (Queue Worker): 2-3 hours
- Phase 5 (DockerService Split): 3-4 hours
- Phase 6 (Constants): 30 minutes
- Unit Tests: 2-3 hours
- Integration Tests: 3-4 hours

Total: ~15-20 hours of development work
