# Feasibility Analysis: GitHub Actions Autoscaler Restructure & Redesign

## Date: February 1, 2026

## Executive Summary

This document analyzes the feasibility of restructuring the GitHub Actions Autoscaler from a monolithic application into a modular, abstracted architecture supporting multiple queue providers and runner backends.

**Key Decision:** Based on [Architecture Decision Analysis](06-application-architecture-decision.md), the application will remain a **single deployable unit with a Mode configuration switch** rather than being split into separate applications.

---

## 1. Current State Analysis

### 1.1 Architecture Overview

The current implementation is a **single ASP.NET Core application** that operates in two modes:
- **Webhook Receiver Mode**: Accepts GitHub webhook payloads and enqueues them
- **Queue Monitor Mode**: Processes queued jobs and manages Docker containers

```
┌─────────────────────────────────────────────────────────────────┐
│                  GithubActionsAutoscaler                        │
├─────────────────────────────────────────────────────────────────┤
│  Program.cs (Entry Point)                                       │
│    ├── WorkflowEndpoints (Webhook → Queue)                      │
│    └── QueueMonitorWorker (Queue → Docker)                      │
├─────────────────────────────────────────────────────────────────┤
│  Services:                                                      │
│    ├── DockerService (Orchestrator)                             │
│    ├── ContainerManager (Container lifecycle)                   │
│    ├── ImageManager (Image pulling)                             │
│    ├── RepositoryFilter (Allowlist/Denylist)                    │
│    └── LabelMatcher (Runner label matching)                     │
├─────────────────────────────────────────────────────────────────┤
│  Dependencies:                                                  │
│    ├── Azure.Storage.Queues (Hard dependency)                   │
│    └── Docker.DotNet (Hard dependency)                          │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Typical Deployment Topology

```
                         ┌─────────────────┐
                         │     GitHub      │
                         │   (Webhooks)    │
                         └────────┬────────┘
                                  │
                                  ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  HOST A (Webhook Receiver)                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │  Autoscaler (Mode=Webhook)                                          │ │
│  │  - Receives webhook from GitHub                                     │ │
│  │  - Enqueues payload to Azure Storage Queue                          │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
                    ┌─────────────────────────┐
                    │   Azure Storage Queue   │
                    │   (Message Broker)      │
                    └─────────────────────────┘
                                  │
          ┌───────────────────────┼───────────────────────┐
          ▼                       ▼                       ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│  HOST B          │    │  HOST C          │    │  HOST D          │
│  (Queue Monitor) │    │  (Queue Monitor) │    │  (Queue Monitor) │
│                  │    │                  │    │                  │
│  - Dequeues jobs │    │  - Dequeues jobs │    │  - Dequeues jobs │
│  - Creates Docker│    │  - Creates Docker│    │  - Creates Docker│
│    runner        │    │    runner        │    │    runner        │
│  - MaxRunners: 4 │    │  - MaxRunners: 8 │    │  - MaxRunners: 2 │
└──────────────────┘    └──────────────────┘    └──────────────────┘
```

**Key Points:**
- Only ONE instance receives webhooks from GitHub (typically behind a reverse proxy)
- Multiple instances monitor the queue and create runner containers
- Each queue monitor can have different `MaxRunners` based on host capacity
- Instances can run on completely different networks
- Some deployments may run webhook + monitor on the same host (`Mode=Both`)

### 1.3 Key Coupling Points

| Component | Coupling Issue | Impact |
|-----------|---------------|--------|
| `QueueMonitorWorker` | Directly depends on `QueueClient` (Azure) | Cannot swap queue providers |
| `QueueMonitorWorker` | Directly depends on `IDockerService` | Cannot swap runner backends |
| `ContainerManager` | Directly uses `DockerClient` | Docker-specific implementation |
| `AppConfiguration` | Flat configuration model + hardcoded defaults | All settings mixed together, defaults not in appsettings.json |
| `Program.cs` | Conditional logic based on config presence | Works but not explicit Mode-based |

### 1.4 Positive Aspects (Foundation to Build On)

- ✅ Already refactored into focused services (ContainerManager, ImageManager, etc.)
- ✅ Interface-based design for testability
- ✅ Existing unit tests (23 tests)
- ✅ Clean separation of Models
- ✅ OpenTelemetry instrumentation already present
- ✅ Already on .NET 10.0
- ✅ Already has implicit mode switching via `UseWebEndpoint` + `AzureStorage` presence

---

## 2. Target Architecture Vision

### 2.1 Application Mode

Based on [Architecture Decision Analysis](06-application-architecture-decision.md), the application will use a **configuration-based Mode switch**:

```csharp
public enum OperationMode
{
    Webhook,      // HTTP endpoints only, enqueues to queue
    QueueMonitor, // Background worker only, processes queue  
    Both          // Full functionality (default for dev/simple deployments)
}
```

### 2.2 High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     GithubActionsAutoscaler (Main App)                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Program.cs                                                           │   │
│  │   if (Mode == Webhook || Mode == Both)                               │   │
│  │       → Register Endpoints, IQueueProvider (send)                    │   │
│  │   if (Mode == QueueMonitor || Mode == Both)                          │   │
│  │       → Register QueueMonitorWorker, IRunnerManager                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │ references
           ┌───────────────────────┼───────────────────────┐
           ▼                       ▼                       ▼
┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────────┐
│  Queue.Azure        │  │  Abstractions       │  │  Runner.Docker          │
│  (Project)          │  │  (Project)          │  │  (Project)              │
├─────────────────────┤  ├─────────────────────┤  ├─────────────────────────┤
│                     │  │                     │  │                         │
│ AzureQueueProvider  │  │ IQueueProvider      │  │ DockerRunnerManager     │
│ AzureQueueMessage   │  │ IQueueMessage       │  │ DockerRunnerInstance    │
│ AzureQueueOptions   │  │ IRunnerManager      │  │ ContainerManager        │
│                     │  │ IRunnerInstance     │  │ ImageManager            │
│ Depends on:         │  │ Workflow models     │  │                         │
│ - Abstractions      │  │ IRepositoryFilter   │  │ Depends on:             │
│ - Azure.Storage.*   │  │ ILabelMatcher       │  │ - Abstractions          │
│                     │  │ InMemoryQueue (test)│  │ - Docker.DotNet         │
└──────────┬──────────┘  │                     │  └────────────┬────────────┘
           │             │ NO external deps    │               │
           │             └──────────┬──────────┘               │
           │                        │                          │
           └────────────────────────┼──────────────────────────┘
                                    │
                      ┌─────────────┴─────────────┐
                      │   Future Providers        │
                      ├───────────────────────────┤
                      │  Queue.RabbitMQ           │

                      │  Runner.Local             │
                      │  Runner.Kubernetes        │
                      └───────────────────────────┘
```

### 2.3 Configuration Structure

**Current (Flat with hardcoded defaults in C#):**
```csharp
// Defaults scattered in AppConfiguration.cs
public string DockerImage { get; set; } = "myoung34/github-runner:latest";
public string DockerHost { get; set; } = "unix:/var/run/docker.sock";
public int MaxRunners { get; set; }  // then adjusted in GetMaxRunners()
```

**Target (Hierarchical in appsettings.json + validation):**

```json
// appsettings.json - ALL defaults live here
{
  "App": {
    "Mode": "Both",
    "GithubToken": "",
    "CoordinatorHostname": "",
    "RepositoryFilter": {
      "AllowlistPrefix": "",
      "Allowlist": [],
      "IsAllowlistExactMatch": true,
      "DenylistPrefix": "",
      "Denylist": [],
      "IsDenylistExactMatch": false
    },
    "Labels": ["self-hosted"],
    "OpenTelemetry": {
      "Enabled": true,
      "ServiceName": "github-actions-autoscaler",
      "OtlpEndpoint": null
    }
  },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": {
      "ConnectionString": "",
      "QueueName": "workflow-jobs"
    }
  },
  "Runner": {
    "Provider": "Docker",
    "MaxRunners": 4,
    "Docker": {
      "Host": "unix:/var/run/docker.sock",
      "Image": "myoung34/github-runner:latest",
      "RegistryToken": "",
      "AutoCheckForImageUpdates": true,
      "ToolCacheVolumeName": "gha-toolcache"
    }
  }
}
```

**Configuration Design Principles:**
1. **No hardcoded defaults in C# code** - all defaults in appsettings.json
2. **IOptions<T> pattern** - proper configuration binding with reload support
3. **Startup validation** - fail fast with clear error messages
4. **Environment variable override** - standard ASP.NET Core precedence

---

## 3. Proposed Project Structure

### 3.1 Solution Layout

```
github-actions-autoscaler/
├── src/
│   ├── GithubActionsAutoscaler/                      # Main application
│   │   ├── Program.cs                                # Mode-based service registration
│   │   ├── appsettings.json                          # ALL defaults live here
│   │   ├── appsettings.Development.json
│   │   ├── GithubActionsAutoscaler.csproj
│   │   │
│   │   ├── Configuration/
│   │   │   ├── AppOptions.cs                         # Root config: Mode, GithubToken, etc.
│   │   │   ├── RepositoryFilterOptions.cs
│   │   │   ├── OpenTelemetryOptions.cs
│   │   │   └── Validation/
│   │   │       └── AppOptionsValidator.cs
│   │   │
│   │   ├── Services/
│   │   │   ├── IWorkflowProcessor.cs
│   │   │   └── WorkflowProcessor.cs                  # Orchestrator
│   │   │
│   │   ├── Endpoints/
│   │   │   └── WorkflowEndpoints.cs
│   │   │
│   │   ├── Workers/
│   │   │   └── QueueMonitorWorker.cs
│   │   │
│   │   └── Extensions/
│   │       ├── OpenTelemetryExtensions.cs
│   │       └── ServiceCollectionExtensions.cs
│   │
│   ├── GithubActionsAutoscaler.Abstractions/         # Shared contracts (NO external deps)
│   │   ├── GithubActionsAutoscaler.Abstractions.csproj
│   │   │
│   │   ├── Models/
│   │   │   ├── Workflow.cs
│   │   │   ├── WorkflowJob.cs
│   │   │   └── Repository.cs
│   │   │
│   │   ├── Queue/
│   │   │   ├── IQueueProvider.cs
│   │   │   ├── IQueueMessage.cs
│   │   │   ├── QueueOptions.cs
│   │   │   └── InMemory/                             # For testing (no external deps)
│   │   │       ├── InMemoryQueueProvider.cs
│   │   │       └── InMemoryQueueMessage.cs
│   │   │
│   │   ├── Runner/
│   │   │   ├── IRunnerManager.cs
│   │   │   ├── IRunnerInstance.cs
│   │   │   ├── RunnerStatus.cs
│   │   │   └── RunnerOptions.cs
│   │   │
│   │   └── Services/
│   │       ├── IRepositoryFilter.cs
│   │       ├── RepositoryFilter.cs
│   │       ├── ILabelMatcher.cs
│   │       └── LabelMatcher.cs
│   │
│   ├── GithubActionsAutoscaler.Queue.Azure/          # Azure Queue implementation
│   │   ├── GithubActionsAutoscaler.Queue.Azure.csproj
│   │   ├── AzureQueueProvider.cs
│   │   ├── AzureQueueMessage.cs
│   │   ├── AzureQueueOptions.cs
│   │   ├── Validation/
│   │   │   └── AzureQueueOptionsValidator.cs
│   │   └── ServiceCollectionExtensions.cs
│   │
│   └── GithubActionsAutoscaler.Runner.Docker/        # Docker runner implementation
│       ├── GithubActionsAutoscaler.Runner.Docker.csproj
│       ├── DockerRunnerManager.cs
│       ├── DockerRunnerInstance.cs
│       ├── DockerRunnerOptions.cs
│       ├── Services/
│       │   ├── IContainerManager.cs
│       │   ├── ContainerManager.cs
│       │   ├── IImageManager.cs
│       │   └── ImageManager.cs
│       ├── Validation/
│       │   └── DockerRunnerOptionsValidator.cs
│       └── ServiceCollectionExtensions.cs
│
├── tests/
│   ├── GithubActionsAutoscaler.Tests.Unit/
│   │   ├── Abstractions/
│   │   │   ├── RepositoryFilterTests.cs
│   │   │   └── LabelMatcherTests.cs
│   │   ├── Queue.Azure/
│   │   │   └── AzureQueueProviderTests.cs
│   │   ├── Runner.Docker/
│   │   │   └── DockerRunnerManagerTests.cs
│   │   ├── Services/
│   │   │   └── WorkflowProcessorTests.cs
│   │   └── Workers/
│   │       └── QueueMonitorWorkerTests.cs
│   │
│   └── GithubActionsAutoscaler.Tests.Integration/
│       ├── QueueIntegrationTests.cs
│       └── DockerIntegrationTests.cs
│
└── docs/
    ├── sample-payload.json
    └── migration-guide.md
```

### 3.2 Project Dependency Graph

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        GithubActionsAutoscaler                              │
│                        (Main Application)                                   │
│                                                                             │
│  References:                                                                │
│    - GithubActionsAutoscaler.Abstractions                                   │
│    - GithubActionsAutoscaler.Queue.Azure                                    │
│    - GithubActionsAutoscaler.Runner.Docker                                  │
│    - Microsoft.AspNetCore.* (web hosting)                                   │
│    - OpenTelemetry.* (instrumentation)                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                │                              │
                ▼                              ▼
┌───────────────────────────┐    ┌───────────────────────────────────────────┐
│  Queue.Azure              │    │  Runner.Docker                            │
│                           │    │                                           │
│  References:              │    │  References:                              │
│    - Abstractions         │    │    - Abstractions                         │
│    - Azure.Storage.Queues │    │    - Docker.DotNet                        │
└───────────────────────────┘    └───────────────────────────────────────────┘
                │                              │
                └──────────────┬───────────────┘
                               ▼
              ┌─────────────────────────────────────┐
              │  GithubActionsAutoscaler.Abstractions│
              │                                     │
              │  References:                        │
              │    - (none / minimal BCL only)      │
              │                                     │
              │  Contains:                          │
              │    - IQueueProvider, IQueueMessage  │
              │    - IRunnerManager, IRunnerInstance│
              │    - Workflow, WorkflowJob models   │
              │    - IRepositoryFilter, ILabelMatcher│
              └─────────────────────────────────────┘
```

### 3.3 Why 4 Projects (Not 1 or 7)

| Consideration | Single Project | 4 Projects | 7+ Projects |
|---------------|----------------|------------|-------------|
| **Dependency enforcement** | ❌ Discipline only | ✅ Compile-time | ✅ Compile-time |
| **Maintenance overhead** | ✅ Minimal | ✅ Acceptable | ❌ High |
| **New provider effort** | ⚠️ Add folder, risk coupling | ✅ Add project, implement interface | ✅ Same |
| **Build complexity** | ✅ Simple | ✅ Simple | ⚠️ More artifacts |
| **External contributor experience** | ⚠️ Must understand full codebase | ✅ Implement interface in isolation | ✅ Same |
| **Testing isolation** | ⚠️ All tests in one project | ✅ Can test provider independently | ✅ Same |

**Decision:** 4 projects provides compile-time architectural enforcement without excessive overhead.

---

## 4. Abstraction Interfaces (Draft Design)

### 4.1 Queue Provider Abstraction

```csharp
namespace GithubActionsAutoscaler.Queue.Abstractions;

public interface IQueueMessage
{
    string MessageId { get; }
    string PopReceipt { get; }
    string Content { get; }
    DateTimeOffset? InsertedOn { get; }
    int DequeueCount { get; }
}

public interface IQueueProvider
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IQueueMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default);
    Task<IQueueMessage?> PeekMessageAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(string content, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(IQueueMessage message, CancellationToken cancellationToken = default);
    Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default);
}
```

### 4.2 Runner Manager Abstraction

```csharp
namespace GithubActionsAutoscaler.Runner.Abstractions;

public enum RunnerStatus
{
    Creating,
    Starting,
    Running,
    Stopping,
    Stopped,
    Failed
}

public interface IRunnerInstance
{
    string Id { get; }
    string Name { get; }
    string Repository { get; }
    long JobRunId { get; }
    RunnerStatus Status { get; }
    DateTimeOffset CreatedAt { get; }
}

public interface IRunnerManager
{
    Task<IReadOnlyList<IRunnerInstance>> GetRunnersAsync(CancellationToken cancellationToken = default);
    Task<int> GetRunnerCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetMaxRunnersAsync();
    Task<bool> CanCreateRunnerAsync(CancellationToken cancellationToken = default);
    Task<IRunnerInstance?> CreateRunnerAsync(
        string repositoryFullName,
        string runnerName,
        long jobRunId,
        CancellationToken cancellationToken = default);
    Task StopRunnerAsync(string runnerId, CancellationToken cancellationToken = default);
    Task CleanupOldRunnersAsync(CancellationToken cancellationToken = default);
    Task WaitForAvailableSlotAsync(CancellationToken cancellationToken = default);
}
```

---

## 5. Requirements Analysis

### 5.1 Functional Requirements

| ID | Requirement | Priority | Complexity |
|----|------------|----------|------------|
| FR-1 | Mode-based operation (Webhook/QueueMonitor/Both) | High | Low |
| FR-2 | Abstract queue provider interface | High | Low |
| FR-3 | Azure Storage Queue implementation | High | Low (existing) |
| FR-4 | Abstract runner manager interface | High | Medium |
| FR-5 | Docker runner implementation | High | Low (existing) |
| FR-6 | Hierarchical configuration model | High | Medium |
| FR-7 | Configuration validation at startup | High | Medium |
| FR-8 | Provider selection via configuration | High | Low |
| FR-9 | All defaults in appsettings.json (not C# code) | High | Low |
| FR-10 | In-memory queue for testing | Medium | Low |
| FR-11 | Local runner provider (future) | Low | High |
| FR-12 | RabbitMQ queue provider (future) | Low | Medium |

### 5.2 Non-Functional Requirements

| ID | Requirement | Priority |
|----|------------|----------|
| NFR-1 | Major version bump (v2.0) - no code-level backward compatibility required | High |
| NFR-2 | Migration documentation for existing users | High |
| NFR-3 | Maintain existing test coverage | High |
| NFR-4 | Preserve OpenTelemetry instrumentation | High |
| NFR-5 | Fail-fast on invalid configuration | High |
| NFR-6 | Documentation for new architecture | Medium |

### 5.3 Options Validation Requirements

```csharp
// Example validation rules
public class AppOptionsValidator : IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(string? name, AppOptions options)
    {
        var failures = new List<string>();

        if (options.Mode is OperationMode.QueueMonitor or OperationMode.Both)
        {
            if (string.IsNullOrWhiteSpace(options.GithubToken))
                failures.Add("GithubToken is required for QueueMonitor mode");
        }

        // Labels must include "self-hosted"
        if (!options.Labels.Contains("self-hosted", StringComparer.OrdinalIgnoreCase))
            failures.Add("Labels must include 'self-hosted'");

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures) 
            : ValidateOptionsResult.Success;
    }
}

public class QueueOptionsValidator : IValidateOptions<QueueOptions>
{
    public ValidateOptionsResult Validate(string? name, QueueOptions options)
    {
        if (options.Provider == "AzureStorageQueue")
        {
            if (string.IsNullOrWhiteSpace(options.AzureStorageQueue.ConnectionString))
                return ValidateOptionsResult.Fail("Azure Storage connection string required");
        }
        return ValidateOptionsResult.Success;
    }
}
```

---

## 6. Risk Assessment & Caveats

### 6.1 Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Configuration migration complexity** | Medium | Medium | Clear migration documentation with examples |
| **Docker.DotNet leaking into abstractions** | Medium | High | Keep `ContainerListResponse` etc. internal to Docker provider |
| **Testing difficulty with Docker interactions** | Medium | Medium | Use Testcontainers; create MockRunnerManager for unit tests |
| **Message serialization compatibility** | Low | Medium | Keep same JSON structure for workflow payloads |
| **Options validation edge cases** | Medium | Low | Comprehensive validation tests |

### 6.2 Caveats & Design Decisions

#### Caveat 1: IContainerManager Exposes Docker Types
The current `IContainerManager.ListContainersAsync()` returns `IList<ContainerListResponse>` which is Docker-specific. This needs to be:
- **Solution**: Keep internal to Docker provider, expose `IRunnerInstance` from `IRunnerManager`

#### Caveat 2: Volume Management is Docker-Specific
Tool cache volumes, work volumes, and Docker socket mounting are Docker-specific concepts. The local runner provider will need different strategies.
- **Decision**: Keep volume management internal to DockerRunnerManager

#### Caveat 3: Label Matching is Provider-Agnostic
Label matching logic (`ILabelMatcher`) should remain in Services since it applies to workflow filtering regardless of runner backend.

#### Caveat 4: Image Management is Docker-Specific
`IImageManager` is inherently Docker-specific and should not be abstracted.
- **Decision**: Keep in Runner/Docker folder only

#### Caveat 5: GitHub Webhook Payload
The webhook payload structure (see [sample-payload.json](sample-payload.json)) is defined by GitHub and should not change. The `Workflow`, `WorkflowJob`, and `Repository` models only need fields we actually use:
- `action` (queued, in_progress, completed, etc.)
- `workflow_job.name`, `workflow_job.labels`, `workflow_job.run_id`
- `repository.full_name`, `repository.name`

#### Caveat 6: Architecture Labels
Current architecture detection uses `RuntimeInformation.ProcessArchitecture`. This is added to configured labels automatically - ensure this behavior is preserved.

---

## 7. Implementation Process

### 7.1 Phase-by-Phase Workflow

For each implementation phase, the following process will be followed:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE X WORKFLOW                                                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. CREATE IMPLEMENTATION PLAN                                          │
│     ├── Detailed, step-by-step instructions                             │
│     ├── Specific file paths and code changes                            │
│     ├── Expected outcomes for each step                                 │
│     └── Skill level: Claude Haiku 4.5 / Gemini Flash 3.0 / equivalent   │
│                                                                         │
│  2. VALIDATE PLAN                                                       │
│     └── Human review and approval                                       │
│                                                                         │
│  3. EXECUTE WITH DEDICATED AGENT                                        │
│     ├── AI agent follows the plan                                       │
│     └── Creates implementation commit                                   │
│                                                                         │
│  4. VERIFY & FIX                                                        │
│     ├── Run tests: dotnet test                                          │
│     ├── Build verification: dotnet build                                │
│     ├── Review code quality                                             │
│     └── Mixed AI/human effort for bug fixes                             │
│                                                                         │
│  5. DOCUMENTATION UPDATE                                                │
│     ├── Update feasibility doc if needed                                │
│     ├── Note any gaps for future phases                                 │
│     └── Update migration guide if applicable                            │
│                                                                         │
│  6. ITERATE TO PHASE X+1                                                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 7.2 Implementation Plan Requirements

Each phase plan must include:

1. **Objective**: Clear statement of what the phase accomplishes
2. **Prerequisites**: What must be complete before starting
3. **Step-by-Step Instructions**: 
   - Numbered steps with specific actions
   - Exact file paths (absolute or relative to project root)
   - Code snippets or templates where helpful
   - Expected state after each step
4. **Verification Steps**: How to confirm the phase is complete
5. **Rollback Instructions**: How to undo if something goes wrong

---

## 8. Phased Implementation Plan

### Phase 0: Preparation (Estimated: 2-3 hours)
- [ ] Create feature branch `feature/v2-restructure`
- [ ] Document current behavior as baseline (existing tests serve this purpose)
- [ ] Ensure all existing tests pass
- [ ] Create initial migration guide skeleton

### Phase 1: Solution Restructure (Estimated: 3-4 hours)
- [ ] Create `GithubActionsAutoscaler.Abstractions` project (class library)
- [ ] Create `GithubActionsAutoscaler.Queue.Azure` project (class library)
- [ ] Create `GithubActionsAutoscaler.Runner.Docker` project (class library)
- [ ] Update solution file with new projects
- [ ] Set up project references (dependency graph)
- [ ] Move Models to Abstractions project
- [ ] Update namespaces
- [ ] Verify solution builds

### Phase 2: Queue Abstraction (Estimated: 4-5 hours)
- [ ] Define `IQueueProvider` and `IQueueMessage` in Abstractions
- [ ] Define `QueueOptions` base class in Abstractions
- [ ] Implement `InMemoryQueueProvider` in Abstractions (for testing)
- [ ] Implement `AzureQueueProvider` in Queue.Azure project
- [ ] Implement `AzureQueueMessage` in Queue.Azure project
- [ ] Create `AzureQueueOptions` with validator
- [ ] Create `AddAzureQueueProvider()` extension method
- [ ] Create `AddInMemoryQueueProvider()` extension method in Abstractions
- [ ] Update `WorkflowEndpoints` to use `IQueueProvider`
- [ ] Update `QueueMonitorWorker` to use `IQueueProvider`
- [ ] Add unit tests for queue providers
- [ ] Verify integration still works

### Phase 3: Runner Abstraction (Estimated: 5-6 hours)
- [ ] Define `IRunnerManager` and `IRunnerInstance` in Abstractions
- [ ] Define `RunnerOptions` base class in Abstractions
- [ ] Move `IRepositoryFilter`, `RepositoryFilter` to Abstractions
- [ ] Move `ILabelMatcher`, `LabelMatcher` to Abstractions
- [ ] Move Docker-specific code to Runner.Docker project
- [ ] Create `DockerRunnerManager` implementing `IRunnerManager`
- [ ] Create `DockerRunnerInstance` implementing `IRunnerInstance`
- [ ] Create `DockerRunnerOptions` with validator
- [ ] Create `AddDockerRunnerProvider()` extension method
- [ ] Create `WorkflowProcessor` service in main app (orchestrator)
- [ ] Update `QueueMonitorWorker` to use `IRunnerManager` via `WorkflowProcessor`
- [ ] Update/migrate existing tests
- [ ] Verify integration still works

### Phase 4: Configuration & Mode (Estimated: 4-5 hours)
- [ ] Create new Options classes (`AppOptions`, etc.) in main app
- [ ] Move all hardcoded defaults to `appsettings.json`
- [ ] Add `Mode` enum (Webhook, QueueMonitor, Both)
- [ ] Implement `IValidateOptions<T>` validators
- [ ] Update `Program.cs` for mode-based service registration
- [ ] Add validation tests
- [ ] Verify all modes work correctly

### Phase 5: Cleanup & Documentation (Estimated: 3-4 hours)
- [ ] Remove old/unused code
- [ ] Ensure OpenTelemetry instrumentation preserved
- [ ] Complete migration guide with before/after examples
- [ ] Update README.md
- [ ] Update AGENTS.md
- [ ] Create docker-compose examples for all modes
- [ ] Update Dockerfile if needed
- [ ] Final integration testing

### Phase 6: Future Providers (Deferred - Not part of v2.0)
- [ ] Local runner provider
- [ ] RabbitMQ queue provider
- [ ] Kubernetes runner provider

---

## 9. Effort Estimation Summary

| Phase | Description | Time Estimate |
|-------|------------|---------------|
| Phase 0 | Preparation | 2-3 hours |
| Phase 1 | Solution Restructure (4 projects) | 3-4 hours |
| Phase 2 | Queue Abstraction | 4-5 hours |
| Phase 3 | Runner Abstraction | 5-6 hours |
| Phase 4 | Configuration & Mode | 4-5 hours |
| Phase 5 | Cleanup & Documentation | 3-4 hours |
| **Total** | | **21-27 hours** |

*Note: Estimates assume AI-assisted implementation with human review.*

---

## 10. Remaining Decision Points

Before beginning Phase 0, the following decisions should be confirmed:

### 10.1 Confirmed Decisions

| Decision | Resolution |
|----------|------------|
| Split vs Single App | **Single app with Mode switch** (see [architecture analysis](06-application-architecture-decision.md)) |
| Project Structure | **4 projects** - Main app, Abstractions, Queue.Azure, Runner.Docker |
| Backward Compatibility | **No code-level compat** - major version bump to v2.0, migration docs only |
| Configuration Defaults | **All defaults in appsettings.json**, not hardcoded in C# |
| Default Mode | **Both** - for easiest quick-start |
| Provider string format | **PascalCase** - e.g., `"AzureStorageQueue"` |
| Validation failure | **Exception at startup** (fail-fast) |
| Legacy config support | **No** - clean break, document migration |
| InMemory queue in v2.0 | **Yes** - useful for integration tests and development |

### 10.2 Remaining Open Questions

All major technical decisions have been resolved. Implementation can proceed to Phase 0.

### 10.3 Minimum Configuration per Mode

**Webhook Mode:**
```json
{
  "App": { "Mode": "Webhook" },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": { "ConnectionString": "..." }
  }
}
```
- No `Runner` section needed
- No `GithubToken` needed (only enqueueing)

**QueueMonitor Mode:**
```json
{
  "App": { 
    "Mode": "QueueMonitor",
    "GithubToken": "..."
  },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": { "ConnectionString": "..." }
  },
  "Runner": {
    "Provider": "Docker"
  }
}
```
- `GithubToken` required (for runner registration)
- Docker defaults sufficient for most cases

**Both Mode:**
- Combines requirements of both modes

### 10.4 Post-Approval Next Steps

Once decisions are confirmed:

1. **Create Phase 0 implementation plan** - detailed, agent-executable instructions
2. **Create feature branch** - `feature/v2-restructure`
3. **Execute Phase 0** with verification
4. **Proceed to Phase 1** following the workflow

---

## 11. Success Criteria

### 11.1 Technical Success Criteria
- [ ] All existing tests pass
- [ ] Application starts in all three modes (Webhook, QueueMonitor, Both)
- [ ] Invalid configuration fails fast with clear error messages
- [ ] Can configure Azure Queue via new config structure
- [ ] Can configure Docker runner via new config structure
- [ ] OpenTelemetry instrumentation preserved
- [ ] No regression in functionality

### 11.2 Quality Success Criteria
- [ ] Code coverage maintained or improved
- [ ] No compiler warnings
- [ ] Nullable reference types fully enabled
- [ ] Options validators have test coverage
- [ ] All public APIs documented

### 11.3 Deployment Success Criteria
- [ ] Docker image builds successfully
- [ ] Can run in existing deployment scenarios (with new config)
- [ ] Migration documentation complete
- [ ] docker-compose examples for all modes

---

## 12. Related Documents

| Document | Description |
|----------|-------------|
| [06-application-architecture-decision.md](06-application-architecture-decision.md) | Detailed analysis of single vs split application decision |
| [sample-payload.json](sample-payload.json) | GitHub webhook payload sample for reference |
| migration-guide.md (to be created) | Step-by-step migration from v1.x to v2.x |

---

## Appendix A: Current Configuration Reference

```csharp
public class AppConfiguration
{
    public bool UseWebEndpoint { get; set; }
    public string AzureStorage { get; set; }
    public string AzureStorageQueue { get; set; }
    public string DockerToken { get; set; }
    public string DockerImage { get; set; }
    public string GithubToken { get; set; }
    public int MaxRunners { get; set; }
    public string RepoAllowlistPrefix { get; set; }
    public string[] RepoAllowlist { get; set; }
    public bool IsRepoAllowlistExactMatch { get; set; }
    public string RepoDenylistPrefix { get; set; }
    public string[] RepoDenylist { get; set; }
    public bool IsRepoDenylistExactMatch { get; set; }
    public string DockerHost { get; set; }
    public string[] Labels { get; set; }
    public OpenTelemetryConfiguration OpenTelemetry { get; set; }
    public bool AutoCheckForImageUpdates { get; set; }
    public string CoordinatorHostname { get; set; }
    public string ToolCacheVolumeName { get; set; }
}
```

---

## Appendix B: GitHub Webhook Payload Reference

See [sample-payload.json](sample-payload.json) for a complete example.

Key fields used by the autoscaler:

```json
{
  "action": "queued",
  "workflow_job": {
    "id": 62082083124,
    "run_id": 21535894442,
    "workflow_name": "CI",
    "name": "Lint",
    "status": "queued",
    "labels": ["self-hosted"],
    "created_at": "2026-01-31T11:26:24Z"
  },
  "repository": {
    "name": "email-folder-manager",
    "full_name": "psimsa/email-folder-manager",
    "private": true
  }
}
```

**Supported `action` values:**
- `queued` - Job is waiting for a runner (triggers container creation)
- `in_progress` - Job is running (informational)
- `completed` - Job finished (triggers volume cleanup)

---

## Appendix C: Target Configuration Schema (Complete)

```json
{
  "App": {
    "Mode": "Both",
    "GithubToken": "",
    "CoordinatorHostname": "",
    "RepositoryFilter": {
      "AllowlistPrefix": "",
      "Allowlist": [],
      "IsAllowlistExactMatch": true,
      "DenylistPrefix": "",
      "Denylist": [],
      "IsDenylistExactMatch": false
    },
    "Labels": ["self-hosted"],
    "OpenTelemetry": {
      "Enabled": true,
      "ServiceName": "github-actions-autoscaler",
      "OtlpEndpoint": null
    }
  },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": {
      "ConnectionString": "",
      "QueueName": "workflow-jobs"
    }
  },
  "Runner": {
    "Provider": "Docker",
    "MaxRunners": 4,
    "Docker": {
      "Host": "unix:/var/run/docker.sock",
      "Image": "myoung34/github-runner:latest",
      "RegistryToken": "",
      "AutoCheckForImageUpdates": true,
      "ToolCacheVolumeName": "gha-toolcache"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

*Document Version: 2.0*
*Last Updated: February 1, 2026*
