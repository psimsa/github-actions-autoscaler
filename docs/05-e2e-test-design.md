# Integration Test Suite Design: Multi-Queue Support

## 1. Objective

To implement a robust, CI-friendly testing strategy that verifies the application's multi-queue support (RabbitMQ and Azure Storage Queues) without requiring Docker-in-Docker or other complex infrastructure.

## 2. Lessons Learned

The initial E2E approach using Docker-in-Docker (DinD) failed because:
- DinD requires privileged containers not reliably available in GitHub Actions
- Testing too many components at once made debugging difficult
- WebApplicationFactory with hosted services has subtle timing issues

## 3. Revised Strategy: Phased Testing

We adopt a phased approach that tests components in isolation before integration:

### Phase 1: Configuration Tests (Unit)
**Location:** `tests/GithubActionsAutoscaler.Tests.Unit/Configuration/`

Verify `AppConfiguration` correctly parses queue provider settings:
- Explicit `QueueProvider=RabbitMQ` returns `QueueProvider.RabbitMQ`
- Explicit `QueueProvider=Azure` returns `QueueProvider.Azure`
- When `AzureStorage` is provided without explicit provider, defaults to Azure
- When nothing configured, defaults to Azure

### Phase 2: Service Registration Tests (Integration)
**Location:** `tests/GithubActionsAutoscaler.Tests.Integration/`

Verify DI container is wired correctly based on configuration:
- RabbitMQ config registers `RabbitMQQueueService`
- Azure config registers `AzureQueueService`
- Queue provider config registers `QueueMonitorWorker`

Uses `WebApplicationFactory` but mocks external dependencies.

### Phase 3: Queue Service Tests (Integration with Testcontainers)
**Location:** `tests/GithubActionsAutoscaler.Tests.Integration/Services/`

Test each `IQueueService` implementation directly:
- `RabbitMQQueueServiceTests` - uses real RabbitMQ container
- `AzureQueueServiceTests` - uses real Azurite container

**No Docker-in-Docker required** - just standard Testcontainers.

### Phase 4: Worker Tests (Integration with mocked Docker)
**Location:** `tests/GithubActionsAutoscaler.Tests.Integration/Workers/`

Test `QueueMonitorWorker` message processing:
- Uses real queue containers (RabbitMQ/Azurite)
- Mocks `IDockerService` to verify it's called correctly
- No actual container creation needed

## 4. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Test Categories                          │
├─────────────────────────────────────────────────────────────┤
│  Unit Tests (no containers)                                  │
│  ├── AppConfigurationTests                                   │
│  └── [existing unit tests]                                   │
├─────────────────────────────────────────────────────────────┤
│  Integration Tests (with Testcontainers)                     │
│  ├── ServiceRegistrationTests (no containers, mock all)      │
│  ├── RabbitMQQueueServiceTests (RabbitMQ container)          │
│  ├── AzureQueueServiceTests (Azurite container)              │
│  └── QueueMonitorWorkerTests (queue containers + mock Docker)│
└─────────────────────────────────────────────────────────────┘
```

## 5. Key Principles

1. **Mock what's hard** - Docker container creation is complex in CI; mock `IDockerService`
2. **Use real queues** - RabbitMQ and Azurite containers are reliable in GitHub Actions
3. **Test in isolation** - Each phase tests one layer of the stack
4. **Fast feedback** - Unit tests run first, integration tests are focused

## 6. Technology Stack

- **Framework:** xUnit
- **Containers:** Testcontainers for .NET (`Testcontainers.RabbitMq`, `Testcontainers.Azurite`)
- **Mocking:** NSubstitute
- **Assertions:** FluentAssertions

## 7. CI/CD Integration

```yaml
- name: Run Unit Tests
  run: dotnet test tests/GithubActionsAutoscaler.Tests.Unit -c Release

- name: Run Integration Tests
  run: dotnet test tests/GithubActionsAutoscaler.Tests.Integration -c Release
```

Both test suites should pass reliably in GitHub Actions without special configuration.

---

## Historical Note

The original design (below) attempted full E2E testing with Docker-in-Docker. This approach was abandoned due to reliability issues in CI environments.

<details>
<summary>Original E2E Design (Deprecated)</summary>

The original approach used:
- Docker-in-Docker (DinD) container for simulating the Docker host
- Full WebApplicationFactory with real hosted services
- Verification of actual container creation

This failed because:
1. DinD requires `--privileged` which isn't available on GitHub-hosted runners
2. WebApplicationFactory timing issues with `IHostedService`
3. Too many moving parts made debugging impossible
</details>
