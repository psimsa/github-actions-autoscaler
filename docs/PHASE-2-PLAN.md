# Phase 2 Implementation Plan: Queue Abstraction

## Date: February 1, 2026

## Objective

Introduce the queue abstraction layer and migrate Azure queue usage to the new architecture.

Targets:
1. Define queue interfaces and options in Abstractions.
2. Implement in-memory queue provider for tests.
3. Implement Azure queue provider in Queue.Azure project.
4. Update main app to use `IQueueProvider` for enqueue/dequeue.

## Prerequisites

✅ Phase 1 complete: Solution restructured with Abstractions, Queue.Azure, Runner.Docker projects
✅ Feature branch: `feature/v2-restructure` checked out
✅ Working directory: Clean with no uncommitted changes

## Implementation Steps

### Step 1: Add Queue Abstractions in Abstractions Project

**Action:** Create queue interfaces, options, and in-memory provider.

**Files to add:**
- `src/GithubActionsAutoscaler.Abstractions/Queue/IQueueMessage.cs`
- `src/GithubActionsAutoscaler.Abstractions/Queue/IQueueProvider.cs`
- `src/GithubActionsAutoscaler.Abstractions/Queue/QueueOptions.cs`
- `src/GithubActionsAutoscaler.Abstractions/Queue/InMemory/InMemoryQueueMessage.cs`
- `src/GithubActionsAutoscaler.Abstractions/Queue/InMemory/InMemoryQueueProvider.cs`
- `src/GithubActionsAutoscaler.Abstractions/Extensions/ServiceCollectionExtensions.cs` (AddInMemoryQueueProvider)

**Expected Result:**
- Abstractions contains queue contracts and in-memory provider.
- No external dependencies added to Abstractions.

---

### Step 2: Implement Azure Queue Provider

**Action:** Add Azure queue provider, options, and validation in Queue.Azure project.

**Files to add:**
- `src/GithubActionsAutoscaler.Queue.Azure/AzureQueueMessage.cs`
- `src/GithubActionsAutoscaler.Queue.Azure/AzureQueueProvider.cs`
- `src/GithubActionsAutoscaler.Queue.Azure/AzureQueueOptions.cs`
- `src/GithubActionsAutoscaler.Queue.Azure/Validation/AzureQueueOptionsValidator.cs`
- `src/GithubActionsAutoscaler.Queue.Azure/ServiceCollectionExtensions.cs` (AddAzureQueueProvider)

**NuGet:**
- Add `Azure.Storage.Queues` to `src/GithubActionsAutoscaler.Queue.Azure/GithubActionsAutoscaler.Queue.Azure.csproj`

---

### Step 3: Update Main App to Use IQueueProvider

**Action:** Replace direct `QueueClient` usage with `IQueueProvider`.

**Files to update:**
- `src/GithubActionsAutoscaler/Endpoints/WorkflowEndpoints.cs`
- `src/GithubActionsAutoscaler/Workers/QueueMonitorWorker.cs`
- `src/GithubActionsAutoscaler/Program.cs`

**Expected Result:**
- Workflow endpoints enqueue via `IQueueProvider`.
- Queue monitor worker dequeues via `IQueueProvider`.
- DI registers Azure provider for now.

---

### Step 4: Update Tests

**Action:** Update unit tests and add coverage for queue providers.

**Files to update/add:**
- `tests/GithubActionsAutoscaler.Tests.Unit/Workers/QueueMonitorWorkerTests.cs`
- Add new test files:
  - `tests/GithubActionsAutoscaler.Tests.Unit/Queue.Azure/AzureQueueProviderTests.cs`
  - `tests/GithubActionsAutoscaler.Tests.Unit/Abstractions/InMemoryQueueProviderTests.cs`

---

### Step 5: Verification

**Action:** Run build and tests.

```bash
dotnet build
dotnet test
```

Expected Result:
- Build succeeds with 0 warnings/errors
- Tests pass (23+)

---

## Rollback Instructions

If something goes wrong:

```bash
# Undo last commit
git reset --hard HEAD~1

# Or restore from backup
git checkout feature/v2-restructure -- .
```

---

## Next Phase

After Phase 2 completion, proceed to **Phase 3: Runner Abstraction** (5-6 hours)

---

*Implementation Plan Version: 1.0*
