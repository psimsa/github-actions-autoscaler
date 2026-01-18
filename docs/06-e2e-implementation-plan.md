# Implementation Plan: Integration Test Suite (Revised)

This document outlines the step-by-step implementation plan for the revised integration test suite. Each phase can be completed autonomously.

## Phase 1: Configuration Unit Tests

**Objective:** Verify `AppConfiguration` correctly determines the queue provider.

### Location
`tests/GithubActionsAutoscaler.Tests.Unit/Configuration/AppConfigurationTests.cs`

### Tests to Implement
1. `GetQueueProvider_WhenExplicitlySetToRabbitMQ_ReturnsRabbitMQ`
2. `GetQueueProvider_WhenExplicitlySetToAzure_ReturnsAzure`
3. `GetQueueProvider_WhenAzureStorageProvided_DefaultsToAzure`
4. `GetQueueProvider_WhenNoConfigProvided_DefaultsToAzure`
5. `GetQueueProvider_IsCaseInsensitive`

### Verification
- `dotnet test tests/GithubActionsAutoscaler.Tests.Unit` passes all tests.

---

## Phase 2: Integration Test Project Setup

**Objective:** Create the integration test project with proper dependencies.

### Steps
1. Create test project:
   ```bash
   dotnet new xunit -n GithubActionsAutoscaler.Tests.Integration -o tests/GithubActionsAutoscaler.Tests.Integration
   dotnet sln add tests/GithubActionsAutoscaler.Tests.Integration
   ```

2. Add project reference:
   ```bash
   dotnet add tests/GithubActionsAutoscaler.Tests.Integration reference src/GithubActionsAutoscaler
   ```

3. Add NuGet packages:
   - `Testcontainers.RabbitMq`
   - `Testcontainers.Azurite`
   - `FluentAssertions`
   - `NSubstitute`
   - `Microsoft.AspNetCore.Mvc.Testing`

### Verification
- `dotnet build` succeeds.

---

## Phase 3: Queue Service Integration Tests

**Objective:** Test each `IQueueService` implementation with real containers. No Docker-in-Docker needed.

### Location
`tests/GithubActionsAutoscaler.Tests.Integration/Services/`

### Fixtures
Create `RabbitMqFixture.cs`:
- Starts RabbitMQ container
- Exposes connection details

Create `AzuriteFixture.cs`:
- Starts Azurite container
- Exposes connection string

### Tests: RabbitMQQueueServiceTests.cs
1. `InitializeAsync_ConnectsSuccessfully`
2. `ReceiveMessageAsync_WhenMessageExists_ReturnsMessage`
3. `ReceiveMessageAsync_WhenQueueEmpty_ReturnsNull`
4. `DeleteMessageAsync_AcknowledgesMessage`
5. `AbandonMessageAsync_RequeuesMessage`

### Tests: AzureQueueServiceTests.cs
1. `InitializeAsync_CreatesQueueIfNotExists`
2. `ReceiveMessageAsync_WhenMessageExists_ReturnsDecodedMessage`
3. `ReceiveMessageAsync_WhenQueueEmpty_ReturnsNull`
4. `DeleteMessageAsync_RemovesMessageFromQueue`

### Verification
- `dotnet test tests/GithubActionsAutoscaler.Tests.Integration` passes all tests.

---

## Phase 4: Worker Integration Tests (Mocked Docker)

**Objective:** Test `QueueMonitorWorker` processes messages correctly, using real queues but mocked Docker service.

### Location
`tests/GithubActionsAutoscaler.Tests.Integration/Workers/`

### Setup
- Use real RabbitMQ/Azurite containers for queue
- Mock `IDockerService` using NSubstitute
- Directly instantiate `QueueMonitorWorker` with dependencies

### Tests: QueueMonitorWorkerTests.cs
1. `ProcessNextMessageAsync_WhenQueuedMessage_CallsProcessWorkflow`
2. `ProcessNextMessageAsync_WhenCompletedMessage_CallsProcessWorkflow`
3. `ProcessNextMessageAsync_WhenProcessingSucceeds_DeletesMessage`
4. `ProcessNextMessageAsync_WhenProcessingFails_AbandonsMessage`
5. `ProcessNextMessageAsync_WhenNoMessage_WaitsAndRetries`

### Verification
- All tests pass reliably.

---

## Phase 5: CI/CD Integration

**Objective:** Add test steps to GitHub Actions workflow.

### Steps
1. Update `.github/workflows/BranchBuild.yml`:
   ```yaml
   - name: Run Unit Tests
     run: dotnet test tests/GithubActionsAutoscaler.Tests.Unit -c Release --no-build
     
   - name: Run Integration Tests
     run: dotnet test tests/GithubActionsAutoscaler.Tests.Integration -c Release --no-build
   ```

### Verification
- Push changes and verify GitHub Actions workflow passes.

---

## Summary

| Phase | Tests | Containers | Mocks |
|-------|-------|------------|-------|
| 1 | Configuration | None | None |
| 2 | (Setup only) | - | - |
| 3 | Queue Services | RabbitMQ, Azurite | None |
| 4 | Worker | RabbitMQ, Azurite | IDockerService |
| 5 | (CI/CD only) | - | - |

This approach:
- Tests the multi-queue feature thoroughly
- Avoids Docker-in-Docker complexity
- Runs reliably in GitHub Actions
- Provides fast feedback
