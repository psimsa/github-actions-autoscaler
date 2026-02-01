# Phase 5 Implementation Plan: Cleanup & Documentation

## Date: February 1, 2026

## Objective

Finalize cleanup and documentation to complete v2.0 restructure.

Targets:
1. Remove unused code and ensure OpenTelemetry instrumentation is preserved.
2. Complete migration guide and README updates.
3. Provide docker-compose examples for all modes.
4. Verify Dockerfile and integration workflows.

## Prerequisites

✅ Phase 4 complete: Configuration + Mode implementation
✅ Feature branch: `feature/v2-restructure` checked out
✅ Working directory: Clean with no uncommitted changes

## Implementation Steps

### Step 1: Cleanup Code

**Action:** Remove unused classes, stale configuration artifacts, and obsolete helpers.

**Files to review/update:**
- `src/GithubActionsAutoscaler/` (Services/Configuration/Extensions)
- `src/GithubActionsAutoscaler.Runner.Docker/`
- `src/GithubActionsAutoscaler.Queue.Azure/`

---

### Step 2: Validate OpenTelemetry

**Action:** Ensure OpenTelemetry instrumentation remains wired through new options.

**Files to review/update:**
- `src/GithubActionsAutoscaler/Extensions/OpenTelemetryExtensions.cs`
- `src/GithubActionsAutoscaler/appsettings.json`

---

### Step 3: Update Migration Guide

**Action:** Document v1 → v2 migration steps with before/after configuration examples.

**Files to update:**
- `docs/migration-guide.md`

---

### Step 4: Update README

**Action:** Document modes, configuration structure, and provider selection.

**Files to update:**
- `README.md`

---

### Step 5: Add docker-compose Examples

**Action:** Provide compose files for Webhook, QueueMonitor, and Both modes.

**Files to add:**
- `docs/docker-compose.webhook.yml`
- `docs/docker-compose.queue-monitor.yml`
- `docs/docker-compose.both.yml`

---

### Step 6: Review Dockerfile

**Action:** Ensure Dockerfile aligns with new config structure and environment variables.

**Files to review/update:**
- `Dockerfile`

---

### Step 7: Verification

```bash
dotnet build
dotnet test
docker build -t github-actions-autoscaler .
```

Expected Result:
- Build and tests succeed
- Docker image builds successfully

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

After Phase 5 completion, only future providers (Phase 6) remain.

---

*Implementation Plan Version: 1.0*
