# Phase 4 Implementation Plan: Configuration & Mode

## Date: February 1, 2026

## Objective

Introduce hierarchical configuration with options binding/validation and add mode-based service registration.

Targets:
1. Replace flat AppConfiguration with options classes (App/Queue/Runner).
2. Move defaults into appsettings.json.
3. Add OperationMode enum (Webhook/QueueMonitor/Both).
4. Add validators for options.
5. Update Program.cs for mode-based service registration.

## Prerequisites

✅ Phase 3 complete: Runner abstraction migrated to Runner.Docker
✅ Feature branch: `feature/v2-restructure` checked out
✅ Working directory: Clean with no uncommitted changes

## Implementation Steps

### Step 1: Add Options Classes (Main App)

**Files to add:**
- `src/GithubActionsAutoscaler/Configuration/AppOptions.cs`
- `src/GithubActionsAutoscaler/Configuration/RepositoryFilterOptions.cs`
- `src/GithubActionsAutoscaler/Configuration/OpenTelemetryOptions.cs`
- `src/GithubActionsAutoscaler/Configuration/QueueOptions.cs`
- `src/GithubActionsAutoscaler/Configuration/RunnerOptions.cs`
- `src/GithubActionsAutoscaler/Configuration/OperationMode.cs`

---

### Step 2: Add Validators

**Files to add:**
- `src/GithubActionsAutoscaler/Configuration/Validation/AppOptionsValidator.cs`
- `src/GithubActionsAutoscaler/Configuration/Validation/QueueOptionsValidator.cs`
- `src/GithubActionsAutoscaler/Configuration/Validation/RunnerOptionsValidator.cs`

---

### Step 3: Update appsettings.json Defaults

**Files to update:**
- `src/GithubActionsAutoscaler/appsettings.json`
- `src/GithubActionsAutoscaler/appsettings.Development.json`

**Expected Result:**
- All defaults defined in JSON; no hardcoded defaults in C#.

---

### Step 4: Update Program.cs for Options Binding + Mode

**Action:** Bind options with `IOptions<T>`, add validators, and register services based on `OperationMode`.

**Files to update:**
- `src/GithubActionsAutoscaler/Program.cs`

---

### Step 5: Update Dependent Services

**Action:** Replace direct `AppConfiguration` usage with options classes.

**Files to update (expected):**
- `src/GithubActionsAutoscaler/Extensions/OpenTelemetryExtensions.cs`
- `src/GithubActionsAutoscaler/Endpoints/WorkflowEndpoints.cs`
- `src/GithubActionsAutoscaler/Workers/QueueMonitorWorker.cs`
- `src/GithubActionsAutoscaler.Runner.Docker/*` (if needed for options binding)

---

### Step 6: Tests

**Action:** Add/update tests for option validation.

**Files to add/update:**
- `tests/GithubActionsAutoscaler.Tests.Unit/Configuration/AppOptionsValidatorTests.cs`
- `tests/GithubActionsAutoscaler.Tests.Unit/Configuration/QueueOptionsValidatorTests.cs`
- `tests/GithubActionsAutoscaler.Tests.Unit/Configuration/RunnerOptionsValidatorTests.cs`

---

### Step 7: Verification

```bash
dotnet build
dotnet test
```

Expected Result:
- Build succeeds with 0 warnings/errors
- Tests pass (31+)

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

After Phase 4 completion, proceed to **Phase 5: Cleanup & Documentation** (3-4 hours)

---

*Implementation Plan Version: 1.0*
