# Architecture Decision: Single Application vs Separate Applications

## Date: February 1, 2026

## Decision Summary

This document analyzes whether the GitHub Actions Autoscaler should be restructured as **two separate applications** or remain a **single application with configuration switches**.

---

## Context

### Current State

The application currently operates as a single deployable unit with two logical components:

1. **Webhook Receiver**: Minimal API endpoints that receive GitHub webhook payloads and enqueue them to Azure Storage Queue
2. **Queue Monitor**: Background worker that dequeues messages and manages Docker runner containers

The current architecture already supports selective activation via configuration:
- `UseWebEndpoint`: Controls whether HTTP endpoints are registered
- Presence of `AzureStorage`: Controls whether `QueueMonitorWorker` starts

### Deployment Patterns

| Scenario | Webhook | Queue Monitor | Notes |
|----------|---------|---------------|-------|
| Multiple hosts, centralized webhook | 1 instance | Multiple instances | Most common production setup |
| Single host, all-in-one | ✓ | ✓ | Development/small deployments |
| Webhook-only relay | ✓ | ✗ | Edge relay to central queue |
| Worker-only (queue consumer) | ✗ | ✓ | Dedicated build hosts |

---

## Option A: Two Separate Applications

### Structure

```
src/
├── GithubActionsAutoscaler.WebhookReceiver/    # Standalone minimal API
│   ├── Program.cs
│   ├── Endpoints/WorkflowEndpoints.cs
│   └── Dockerfile.webhook
│
├── GithubActionsAutoscaler.QueueMonitor/       # Standalone worker service
│   ├── Program.cs
│   ├── Workers/QueueMonitorWorker.cs
│   └── Dockerfile.monitor
│
└── GithubActionsAutoscaler.Shared/             # Shared library
    ├── Models/
    ├── Configuration/
    └── Services/
```

### Deployment

```yaml
# docker-compose.yml
services:
  webhook:
    image: ghcr.io/owner/autoscaler-webhook:latest
    ports:
      - "8080:8080"
    environment:
      - AzureStorage=${AZURE_STORAGE}
      - AzureStorageQueue=${QUEUE_NAME}

  monitor-host1:
    image: ghcr.io/owner/autoscaler-monitor:latest
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - AzureStorage=${AZURE_STORAGE}
      - AzureStorageQueue=${QUEUE_NAME}
      - MaxRunners=4
```

---

## Option B: Single Application with Configuration Switch

### Structure

```
src/
└── GithubActionsAutoscaler/
    ├── Program.cs                 # Conditional service registration
    ├── Configuration/
    │   └── AppConfiguration.cs    # Mode: Webhook | QueueMonitor | Both
    ├── Endpoints/
    ├── Workers/
    └── Dockerfile
```

### Deployment

```yaml
# docker-compose.yml
services:
  webhook:
    image: ghcr.io/owner/autoscaler:latest
    ports:
      - "8080:8080"
    environment:
      - Mode=Webhook
      - AzureStorage=${AZURE_STORAGE}

  monitor-host1:
    image: ghcr.io/owner/autoscaler:latest
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - Mode=QueueMonitor
      - AzureStorage=${AZURE_STORAGE}
      - MaxRunners=4
```

---

## Detailed Analysis

### 1. Deployment Complexity

| Aspect | Option A (Separate) | Option B (Single) |
|--------|---------------------|-------------------|
| **Container Images** | 2 images to build/push/maintain | 1 image |
| **Image Size** | Webhook: ~80MB (minimal), Monitor: ~120MB (Docker.DotNet) | ~120MB (includes everything) |
| **Registry Management** | 2 tags to track per release | 1 tag per release |
| **docker-compose** | Different images per service | Same image, different env vars |
| **Kubernetes** | Separate Deployments + manifests | Single Deployment template, different ConfigMaps |
| **Helm Chart** | More templates, conditional logic | Simpler, values-driven |

#### Winner: **Option B** - Less operational overhead

### 2. Development & Maintenance

| Aspect | Option A (Separate) | Option B (Single) |
|--------|---------------------|-------------------|
| **Code Organization** | Clear physical boundaries | Requires discipline to maintain logical separation |
| **Shared Code** | Explicit shared library project | Internally referenced, easier refactoring |
| **IDE Experience** | Multiple startup projects | Single startup with configuration |
| **Debugging** | Run both apps simultaneously | Run once, test both modes |
| **CI Pipeline** | Build/test/publish 2 artifacts | Build/test/publish 1 artifact |
| **Version Sync** | Must keep versions aligned | Automatically synchronized |
| **Breaking Changes** | Risk of shared library drift | Single codebase, changes are atomic |

#### Winner: **Option B** - Simpler development workflow

### 3. Testing Strategy

| Aspect | Option A (Separate) | Option B (Single) |
|--------|---------------------|-------------------|
| **Unit Tests** | Each app has focused tests | All tests in one project |
| **Integration Tests** | Need to test app interaction | Single app, mode-based tests |
| **E2E Testing** | Complex: spin up both apps | Simple: one app in "Both" mode |
| **Test Coverage** | Potentially duplicate setup code | Shared test infrastructure |

#### Winner: **Option B** - Simpler test setup, "Both" mode perfect for integration tests

### 4. Operational Concerns

| Aspect | Option A (Separate) | Option B (Single) |
|--------|---------------------|-------------------|
| **Updates** | Roll out webhook and monitor independently | Same image everywhere, guaranteed compatibility |
| **Rolling Restarts** | Fine-grained control | Same behavior via orchestrator |
| **Monitoring** | Different service names/dashboards | Same service, different instance labels |
| **Log Aggregation** | Filter by service name | Filter by Mode label |
| **Resource Utilization** | Webhook: minimal resources, Monitor: more | Uniform resource profile |
| **Failure Isolation** | Webhook crash doesn't affect monitors | N/A - they're already separate instances |

#### Analysis

Failure isolation is often cited as a benefit of separate applications, but in this architecture **it's a non-issue**:

- Webhook and QueueMonitor instances are **already deployed separately** in production
- They communicate **only via the queue** - no in-process coupling
- A webhook crash doesn't affect running monitors (and vice versa)
- The queue provides natural buffering and decoupling

#### Winner: **Tie** - Both achieve equivalent operational characteristics

### 5. Resource Efficiency

| Mode | Option A (Separate) | Option B (Single) |
|------|---------------------|-------------------|
| **Webhook-only** | Minimal image, no Docker.DotNet loaded | Full image, but unused services not registered |
| **Monitor-only** | No HTTP stack loaded | HTTP stack loaded but not listening |
| **Both modes** | N/A - must run 2 containers | Single container, all capabilities |

**Memory Comparison (estimated):**

| Scenario | Option A | Option B |
|----------|----------|----------|
| Webhook-only | ~40MB | ~60MB |
| Monitor-only | ~80MB | ~80MB |
| Combined | ~120MB (2 processes) | ~80MB (1 process) |

#### Winner: **Option B** - More efficient for combined deployments, negligible difference otherwise

### 6. User Experience

| Aspect | Option A (Separate) | Option B (Single) |
|--------|---------------------|-------------------|
| **Quick Start** | "Which image do I pull?" | Pull one image, set Mode |
| **Documentation** | Document 2 apps, explain relationship | Document 1 app with 3 modes |
| **Migration Path** | Breaking change - new images | Non-breaking - add Mode config |
| **Mental Model** | "Two tools that work together" | "One tool with flexible deployment" |
| **First-time Setup** | Start 2 containers, configure both | Start 1 container with Mode=Both |

#### Winner: **Option B** - Lower barrier to entry, smoother migration

### 7. Scaling Considerations

Both options support the same scaling patterns:

```
                    ┌─────────────┐
                    │   GitHub    │
                    └──────┬──────┘
                           │ webhook
                           ▼
                    ┌─────────────┐
                    │  Webhook    │  ← Single instance (or HA pair)
                    │  Instance   │
                    └──────┬──────┘
                           │ enqueue
                           ▼
                    ┌─────────────┐
                    │   Azure     │
                    │   Queue     │
                    └──────┬──────┘
                           │ dequeue
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
    ┌────────────┐  ┌────────────┐  ┌────────────┐
    │  Monitor   │  │  Monitor   │  │  Monitor   │
    │  Host A    │  │  Host B    │  │  Host C    │
    └────────────┘  └────────────┘  └────────────┘
```

The scaling model is identical - the only difference is whether each box is a different image or the same image with different Mode.

#### Winner: **Tie** - Architecturally equivalent

---

## Hybrid Option: Supporting Both

It's possible to support both deployment models with minimal overhead:

### Implementation Strategy

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var config = AppConfiguration.FromConfiguration(builder.Configuration);

// Mode-based service registration
if (config.Mode is OperationMode.Webhook or OperationMode.Both)
{
    builder.Services.AddEndpointsApiExplorer();
    // Webhook-specific registrations
}

if (config.Mode is OperationMode.QueueMonitor or OperationMode.Both)
{
    builder.Services.AddSingleton<IDockerService, DockerService>();
    builder.Services.AddHostedService<QueueMonitorWorker>();
    // Monitor-specific registrations
}

// Shared services
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<IRepositoryFilter, RepositoryFilter>();

var app = builder.Build();

if (config.Mode is OperationMode.Webhook or OperationMode.Both)
{
    app.MapWorkflowEndpoints();
}

app.Run();
```

### Build Targets (Optional)

If separate images are desired in the future:

```dockerfile
# Dockerfile.webhook
FROM base AS webhook
ENV Mode=Webhook
EXPOSE 8080

# Dockerfile.monitor  
FROM base AS monitor
ENV Mode=QueueMonitor
# No EXPOSE - no HTTP
```

Same source, different default configuration.

### Additional Effort

| Task | Effort |
|------|--------|
| Add `Mode` enum and config | 30 minutes |
| Refactor `Program.cs` conditional registration | 1-2 hours |
| Add separate Dockerfiles (optional) | 30 minutes |
| Documentation updates | 1 hour |
| **Total** | **~4 hours** |

---

## Risk Assessment

### Option A Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Version drift between apps | Medium | High | Strict version coupling in CI |
| Shared library breaking changes | Medium | Medium | Semantic versioning, careful API design |
| Increased CI complexity | High | Low | Matrix builds, but more maintenance |
| User confusion (which image?) | Medium | Medium | Clear documentation |

### Option B Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Mode misconfiguration | Low | Medium | Validation at startup, clear error messages |
| Unused code loaded | Low | Low | Minimal overhead, lazy initialization |
| Feature coupling temptation | Medium | Medium | Maintain logical separation, code reviews |

---

## Recommendation

### Primary Recommendation: **Option B (Single Application with Configuration Switch)**

#### Justification

1. **Already Working This Way**: The current codebase already implements mode-based behavior via `UseWebEndpoint` and conditional `QueueMonitorWorker` registration. This is a refinement, not a redesign.

2. **Operational Simplicity**: One image to build, test, scan, sign, and distribute. Version compatibility is guaranteed by definition.

3. **User Experience**: New users can start with `Mode=Both` and evolve to distributed deployment without changing images - just configuration.

4. **Migration Path**: Existing deployments continue working. Add `Mode` config gradually.

5. **Development Velocity**: Single codebase, single test suite, single CI pipeline. Changes are atomic and consistent.

6. **Cost Efficiency**: Less container registry storage, simpler Kubernetes manifests, reduced operational cognitive load.

### When Option A Makes Sense

Consider separate applications if:

- **Security isolation is mandatory**: E.g., webhook receiver in DMZ with minimal attack surface
- **Vastly different base images needed**: E.g., monitor needs GPU support
- **Independent release cycles required**: E.g., webhook API versioning independent of runner logic
- **Team ownership boundaries**: Separate teams own separate components

### Implementation Guidance

```csharp
// Configuration/OperationMode.cs
public enum OperationMode
{
    Webhook,      // HTTP endpoints only, enqueues to queue
    QueueMonitor, // Background worker only, processes queue
    Both          // Full functionality (default for development)
}

// Configuration/AppConfiguration.cs
public OperationMode Mode { get; set; } = OperationMode.Both;
```

### Docker Deployment Examples

```yaml
# Development / Small deployment
services:
  autoscaler:
    image: ghcr.io/owner/autoscaler:latest
    environment:
      - Mode=Both
    ports:
      - "8080:8080"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock

---

# Production - Distributed
services:
  webhook:
    image: ghcr.io/owner/autoscaler:latest
    environment:
      - Mode=Webhook
    ports:
      - "8080:8080"

  monitor:
    image: ghcr.io/owner/autoscaler:latest
    environment:
      - Mode=QueueMonitor
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    deploy:
      replicas: 3
```

---

## Decision Record

| Attribute | Value |
|-----------|-------|
| **Decision** | Single Application with Configuration Switch (Option B) |
| **Status** | Proposed |
| **Deciders** | [TBD] |
| **Date** | February 1, 2026 |
| **Consequences** | Lower operational complexity, smoother migration, single artifact to maintain |

---

## Summary Table

| Criterion | Option A (Separate) | Option B (Single) | Winner |
|-----------|---------------------|-------------------|--------|
| Deployment Complexity | Higher | Lower | B |
| Development Workflow | Complex | Simple | B |
| Testing Strategy | More setup | Simpler | B |
| Operational Concerns | Equivalent | Equivalent | Tie |
| Resource Efficiency | Slightly better isolation | More efficient combined | B |
| User Experience | Steeper learning curve | Smoother | B |
| Scaling | Equivalent | Equivalent | Tie |
| Migration from Current | Breaking | Non-breaking | B |
| Future Flexibility | High | High (can split later if needed) | Tie |

**Final Score: Option B wins 5-0 (with 4 ties)**

---

*Document Version: 1.0*  
*Author: Architecture Analysis*  
*Last Updated: February 1, 2026*
