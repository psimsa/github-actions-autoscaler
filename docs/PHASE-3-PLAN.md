# Phase 3 Implementation Plan: Runner Abstraction

## Date: February 1, 2026

## Objective

Introduce runner abstractions, migrate Docker runner implementation into Runner.Docker project, and add a workflow orchestrator to decouple queue processing from runner management.

Targets:
1. Define runner interfaces and options in Abstractions.
2. Move Docker-specific runner code to Runner.Docker.
3. Add a WorkflowProcessor service to orchestrate queue events.
4. Update QueueMonitorWorker to use IRunnerManager via WorkflowProcessor.

## Prerequisites

✅ Phase 2 complete: Queue abstraction implemented and Azure provider migrated
✅ Feature branch: `feature/v2-restructure` checked out
✅ Working directory: Clean with no uncommitted changes

## Implementation Steps

### Step 1: Add Runner Abstractions in Abstractions Project

**Action:** Create runner contracts and options.

**Files to add:**
- `src/GithubActionsAutoscaler.Abstractions/Runner/RunnerStatus.cs`
- `src/GithubActionsAutoscaler.Abstractions/Runner/IRunnerInstance.cs`
- `src/GithubActionsAutoscaler.Abstractions/Runner/IRunnerManager.cs`
- `src/GithubActionsAutoscaler.Abstractions/Runner/RunnerOptions.cs`

---

### Step 2: Move Docker Runner Code to Runner.Docker

**Action:** Relocate Docker-specific services.

**Files to move:**
- `src/GithubActionsAutoscaler/Services/IContainerManager.cs`
- `src/GithubActionsAutoscaler/Services/ContainerManager.cs`
- `src/GithubActionsAutoscaler/Services/IImageManager.cs`
- `src/GithubActionsAutoscaler/Services/ImageManager.cs`

**Files to add:**
- `src/GithubActionsAutoscaler.Runner.Docker/DockerRunnerManager.cs`
- `src/GithubActionsAutoscaler.Runner.Docker/DockerRunnerInstance.cs`
- `src/GithubActionsAutoscaler.Runner.Docker/DockerRunnerOptions.cs`
- `src/GithubActionsAutoscaler.Runner.Docker/Validation/DockerRunnerOptionsValidator.cs`
- `src/GithubActionsAutoscaler.Runner.Docker/ServiceCollectionExtensions.cs`

---

### Step 3: Add Workflow Processor Orchestrator

**Action:** Add orchestration service in main app.

**Files to add:**
- `src/GithubActionsAutoscaler/Services/IWorkflowProcessor.cs`
- `src/GithubActionsAutoscaler/Services/WorkflowProcessor.cs`

**Update:**
- Register in `src/GithubActionsAutoscaler/Program.cs`

---

### Step 4: Update QueueMonitorWorker

**Action:** Replace direct DockerService usage with WorkflowProcessor + IRunnerManager.

**Files to update:**
- `src/GithubActionsAutoscaler/Workers/QueueMonitorWorker.cs`

---

### Step 5: Update Tests

**Action:** Update unit tests for new runner abstraction.

**Files to update/add:**
- `tests/GithubActionsAutoscaler.Tests.Unit/Services/DockerServiceTests.cs` (migrate to new runner manager)
- Add `tests/GithubActionsAutoscaler.Tests.Unit/Runner.Docker/DockerRunnerManagerTests.cs`
- Add `tests/GithubActionsAutoscaler.Tests.Unit/Services/WorkflowProcessorTests.cs`

---

### Step 6: Verification

```bash
dotnet build
dotnet test
```

Expected Result:
- Build succeeds with 0 warnings/errors
- Tests pass (29+)

---

## Rollback Instructions

```bash
# Undo last commit
git reset --hard HEAD~1

# Or restore from backup
git checkout feature/v2-restructure -- .
```

---

## Next Phase

After Phase 3 completion, proceed to **Phase 4: Configuration & Mode** (4-5 hours)

---

*Implementation Plan Version: 1.0*
