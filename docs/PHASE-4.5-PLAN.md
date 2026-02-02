# Phase 4.5 Implementation Plan: Testing & Telemetry Alignment

## Date: February 1, 2026

## Objective

Remove FluentAssertions usage, align logging with activity-based telemetry, review OpenTelemetry usage for best practices, and introduce a first set of custom metrics (jobs and queue depth).

Targets:
1. Replace FluentAssertions in tests with built-in assertions.
2. Reduce Information-level logging to lifecycle events only.
3. Add activity events for non-lifecycle operational messages.
4. Review OpenTelemetry configuration and suggest best-practice improvements.
5. Add custom metrics for queue depth and workflow processing outcomes.

## Prerequisites

✅ Phase 4 complete: Configuration + Mode implementation
✅ Feature branch: `feature/v2-restructure` checked out
✅ Working directory: Clean with no uncommitted changes

## Implementation Steps

### Step 1: Remove FluentAssertions from Tests

**Action:** Replace FluentAssertions with built-in assertions.

**Files to update:**
- `tests/GithubActionsAutoscaler.Tests.Unit/**/*.cs`

**Expected Result:**
- Tests use `Assert.*` instead of FluentAssertions.
- Remove FluentAssertions package reference if present.

---

### Step 2: Add Custom Metrics

**Action:** Define a dedicated `Meter` and record counters/gauges.

**Metrics to add (initial set):**
- `autoscaler.workflow.jobs.received` (counter, tags: `action`, `mode`)
- `autoscaler.workflow.jobs.started` (counter, tags: `repository`)
- `autoscaler.workflow.jobs.completed` (counter, tags: `repository`)
- `autoscaler.queue.messages.deleted` (counter)
- `autoscaler.queue.messages.failed` (counter)
- `autoscaler.queue.depth` (observable gauge)
- `autoscaler.runners.active` (observable gauge)

**Files to add/update (expected):**
- `src/GithubActionsAutoscaler/Extensions/OpenTelemetryExtensions.cs`
- `src/GithubActionsAutoscaler/Workers/QueueMonitorWorker.cs`
- `src/GithubActionsAutoscaler/Services/WorkflowProcessor.cs`
- `src/GithubActionsAutoscaler.Runner.Docker/DockerRunnerManager.cs`

**Implementation detail:**
- Use `Meter` from `System.Diagnostics.Metrics`.
- Expose gauges via `ObservableGauge` reading from providers.
- Add tags for repository/action where helpful.

---

### Step 3: Logging vs Activity Events

**Action:** Convert non-lifecycle Info logs to activity events.

**Guidelines:**
- Keep Info logs for lifecycle milestones (service start/stop, mode startup, provider init).
- Use `Activity.Current?.AddEvent(...)` for operational messages (per job, per container).

**Files to review/update (expected):**
- `src/GithubActionsAutoscaler/Workers/QueueMonitorWorker.cs`
- `src/GithubActionsAutoscaler/Services/WorkflowProcessor.cs`
- `src/GithubActionsAutoscaler.Runner.Docker/*`

---

### Step 4: OpenTelemetry Review

**Action:** Review OpenTelemetry usage and propose changes.

**Deliverable:**
- A short best-practice note in the response (and code changes if appropriate).

---

### Step 5: Verification

```bash
dotnet build
dotnet test
```

Expected Result:
- Build succeeds
- Tests pass

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

After Phase 4.5 completion, proceed to **Phase 5: Cleanup & Documentation** (3-4 hours)

---

*Implementation Plan Version: 1.0*
