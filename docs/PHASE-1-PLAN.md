# Phase 1 Implementation Plan: Solution Restructure

## Date: February 1, 2026

## Objective

Create 4 new class library projects and establish the foundation for provider abstractions:
1. `GithubActionsAutoscaler.Abstractions` - Interfaces, Models, Shared Services
2. `GithubActionsAutoscaler.Queue.Azure` - Azure Queue implementation
3. `GithubActionsAutoscaler.Runner.Docker` - Docker runner implementation

Move Models and shared code to Abstractions. Set up project references so the dependency graph is:
```
Main App → Queue.Azure, Runner.Docker, Abstractions
Queue.Azure → Abstractions
Runner.Docker → Abstractions
Abstractions → (no external dependencies except BCL)
```

## Prerequisites

✅ Phase 0 complete: All 23 tests passing
✅ Feature branch: `feature/v2-restructure` checked out
✅ Working directory: Clean with no uncommitted changes

## Implementation Steps

### Step 1: Create GithubActionsAutoscaler.Abstractions Project

**Action:** Create new class library project

```bash
cd c:\dev\psimsa\github-actions-autoscaler\src
dotnet new classlib -n GithubActionsAutoscaler.Abstractions -f net10.0
```

**Expected Result:**
- New folder: `src/GithubActionsAutoscaler.Abstractions/`
- New file: `src/GithubActionsAutoscaler.Abstractions/GithubActionsAutoscaler.Abstractions.csproj`
- New file: `src/GithubActionsAutoscaler.Abstractions/Class1.cs` (delete this)

**Verification:**
```bash
ls src/GithubActionsAutoscaler.Abstractions/
```

---

### Step 2: Create GithubActionsAutoscaler.Queue.Azure Project

**Action:** Create new class library project

```bash
cd c:\dev\psimsa\github-actions-autoscaler\src
dotnet new classlib -n GithubActionsAutoscaler.Queue.Azure -f net10.0
```

**Expected Result:**
- New folder: `src/GithubActionsAutoscaler.Queue.Azure/`
- New file: `src/GithubActionsAutoscaler.Queue.Azure/GithubActionsAutoscaler.Queue.Azure.csproj`
- New file: `src/GithubActionsAutoscaler.Queue.Azure/Class1.cs` (delete this)

**Verification:**
```bash
ls src/GithubActionsAutoscaler.Queue.Azure/
```

---

### Step 3: Create GithubActionsAutoscaler.Runner.Docker Project

**Action:** Create new class library project

```bash
cd c:\dev\psimsa\github-actions-autoscaler\src
dotnet new classlib -n GithubActionsAutoscaler.Runner.Docker -f net10.0
```

**Expected Result:**
- New folder: `src/GithubActionsAutoscaler.Runner.Docker/`
- New file: `src/GithubActionsAutoscaler.Runner.Docker/GithubActionsAutoscaler.Runner.Docker.csproj`
- New file: `src/GithubActionsAutoscaler.Runner.Docker/Class1.cs` (delete this)

**Verification:**
```bash
ls src/GithubActionsAutoscaler.Runner.Docker/
```

---

### Step 4: Clean Up Generated Class1.cs Files

**Action:** Delete `Class1.cs` from each new project

```bash
rm src/GithubActionsAutoscaler.Abstractions/Class1.cs
rm src/GithubActionsAutoscaler.Queue.Azure/Class1.cs
rm src/GithubActionsAutoscaler.Runner.Docker/Class1.cs
```

**Verification:** Verify each project has no `Class1.cs`

---

### Step 5: Add Project References to Solution

**Action:** Re-build solution to discover new projects

```bash
cd c:\dev\psimsa\github-actions-autoscaler
dotnet sln add src/GithubActionsAutoscaler.Abstractions/GithubActionsAutoscaler.Abstractions.csproj
dotnet sln add src/GithubActionsAutoscaler.Queue.Azure/GithubActionsAutoscaler.Queue.Azure.csproj
dotnet sln add src/GithubActionsAutoscaler.Runner.Docker/GithubActionsAutoscaler.Runner.Docker.csproj
```

**Verification:**
```bash
dotnet sln list
```

Should show:
- GithubActionsAutoscaler (main app)
- GithubActionsAutoscaler.Abstractions
- GithubActionsAutoscaler.Queue.Azure
- GithubActionsAutoscaler.Runner.Docker
- GithubActionsAutoscaler.Tests.Unit
- GithubActionsAutoscaler.Tests.Integration

---

### Step 6: Set Up Project References (Dependency Graph)

**Action:** Add project references to establish dependency graph

**6a. Main app references Abstractions:**
```bash
cd c:\dev\psimsa\github-actions-autoscaler\src\GithubActionsAutoscaler
dotnet add reference ../GithubActionsAutoscaler.Abstractions/GithubActionsAutoscaler.Abstractions.csproj
dotnet add reference ../GithubActionsAutoscaler.Queue.Azure/GithubActionsAutoscaler.Queue.Azure.csproj
dotnet add reference ../GithubActionsAutoscaler.Runner.Docker/GithubActionsAutoscaler.Runner.Docker.csproj
```

**6b. Queue.Azure references Abstractions:**
```bash
cd c:\dev\psimsa\github-actions-autoscaler\src\GithubActionsAutoscaler.Queue.Azure
dotnet add reference ../GithubActionsAutoscaler.Abstractions/GithubActionsAutoscaler.Abstractions.csproj
```

**6c. Runner.Docker references Abstractions:**
```bash
cd c:\dev\psimsa\github-actions-autoscaler\src\GithubActionsAutoscaler.Runner.Docker
dotnet add reference ../GithubActionsAutoscaler.Abstractions/GithubActionsAutoscaler.Abstractions.csproj
```

**Verification:**
```bash
cd c:\dev\psimsa\github-actions-autoscaler
dotnet build
```

Should compile successfully with 0 warnings/errors.

---

### Step 7: Move Models to Abstractions Project

**Action:** Move the following files from main app to Abstractions

**Files to move:**
- `src/GithubActionsAutoscaler/Models/Workflow.cs` → `src/GithubActionsAutoscaler.Abstractions/Models/Workflow.cs`
- `src/GithubActionsAutoscaler/Models/WorkflowJob.cs` → `src/GithubActionsAutoscaler.Abstractions/Models/WorkflowJob.cs`
- `src/GithubActionsAutoscaler/Models/Repository.cs` → `src/GithubActionsAutoscaler.Abstractions/Models/Repository.cs`

**Steps:**
1. Create `Models` folder in Abstractions:
   ```bash
   mkdir src/GithubActionsAutoscaler.Abstractions/Models
   ```

2. Copy the files:
   ```bash
   cp src/GithubActionsAutoscaler/Models/Workflow.cs src/GithubActionsAutoscaler.Abstractions/Models/
   cp src/GithubActionsAutoscaler/Models/WorkflowJob.cs src/GithubActionsAutoscaler.Abstractions/Models/
   cp src/GithubActionsAutoscaler/Models/Repository.cs src/GithubActionsAutoscaler.Abstractions/Models/
   ```

3. Update namespace in each moved file from `namespace GithubActionsAutoscaler.Models;` to `namespace GithubActionsAutoscaler.Abstractions.Models;`

4. Delete original files:
   ```bash
   rm src/GithubActionsAutoscaler/Models/Workflow.cs
   rm src/GithubActionsAutoscaler/Models/WorkflowJob.cs
   rm src/GithubActionsAutoscaler/Models/Repository.cs
   ```

5. Update imports in main app:
   - In any main app files using models, change:
     - `using GithubActionsAutoscaler.Models;` → `using GithubActionsAutoscaler.Abstractions.Models;`

**Files to check and update:**
- `src/GithubActionsAutoscaler/Endpoints/WorkflowEndpoints.cs` - uses `Workflow`
- `src/GithubActionsAutoscaler/Workers/QueueMonitorWorker.cs` - uses `Workflow`
- `src/GithubActionsAutoscaler/Program.cs` - may need updates

**Verification:**
```bash
cd c:\dev\psimsa\github-actions-autoscaler
dotnet build
```

Should compile successfully.

---

### Step 8: Move Shared Services to Abstractions

**Action:** Move repository filtering and label matching to Abstractions

**Files to move:**
- `src/GithubActionsAutoscaler/Services/IRepositoryFilter.cs`
- `src/GithubActionsAutoscaler/Services/RepositoryFilter.cs`
- `src/GithubActionsAutoscaler/Services/ILabelMatcher.cs`
- `src/GithubActionsAutoscaler/Services/LabelMatcher.cs`

**Steps:**
1. Create `Services` folder in Abstractions:
   ```bash
   mkdir src/GithubActionsAutoscaler.Abstractions/Services
   ```

2. Copy the files:
   ```bash
   cp src/GithubActionsAutoscaler/Services/IRepositoryFilter.cs src/GithubActionsAutoscaler.Abstractions/Services/
   cp src/GithubActionsAutoscaler/Services/RepositoryFilter.cs src/GithubActionsAutoscaler.Abstractions/Services/
   cp src/GithubActionsAutoscaler/Services/ILabelMatcher.cs src/GithubActionsAutoscaler.Abstractions/Services/
   cp src/GithubActionsAutoscaler/Services/LabelMatcher.cs src/GithubActionsAutoscaler.Abstractions/Services/
   ```

3. Update namespace in each file from `namespace GithubActionsAutoscaler.Services;` to `namespace GithubActionsAutoscaler.Abstractions.Services;`

4. Update imports in moved files:
   - In `RepositoryFilter.cs` and `LabelMatcher.cs`: change configuration imports (if any) to reference main app config

5. Delete original files:
   ```bash
   rm src/GithubActionsAutoscaler/Services/IRepositoryFilter.cs
   rm src/GithubActionsAutoscaler/Services/RepositoryFilter.cs
   rm src/GithubActionsAutoscaler/Services/ILabelMatcher.cs
   rm src/GithubActionsAutoscaler/Services/LabelMatcher.cs
   ```

6. Update imports in main app:
   - In `Program.cs`, `DockerService.cs`, and any other files using these services:
     - Change: `using GithubActionsAutoscaler.Services;` → `using GithubActionsAutoscaler.Abstractions.Services;`

**Verification:**
```bash
cd c:\dev\psimsa\github-actions-autoscaler
dotnet build
```

Should compile successfully.

---

### Step 9: Update Test Project Reference

**Action:** Update test project to reference Abstractions

```bash
cd c:\dev\psimsa\github-actions-autoscaler\tests\GithubActionsAutoscaler.Tests.Unit
dotnet add reference ../../src/GithubActionsAutoscaler.Abstractions/GithubActionsAutoscaler.Abstractions.csproj
```

**Expected Result:**
Tests can now reference types from `GithubActionsAutoscaler.Abstractions.*` namespaces.

---

### Step 10: Update Test Imports

**Action:** Update all test imports to use new namespaces

Find and update the following in test files:
- `using GithubActionsAutoscaler.Models;` → `using GithubActionsAutoscaler.Abstractions.Models;`
- `using GithubActionsAutoscaler.Services;` → `using GithubActionsAutoscaler.Abstractions.Services;`

**Files to check:**
- `tests/GithubActionsAutoscaler.Tests.Unit/Services/RepositoryFilterTests.cs`
- `tests/GithubActionsAutoscaler.Tests.Unit/Services/LabelMatcherTests.cs`
- `tests/GithubActionsAutoscaler.Tests.Unit/Workers/QueueMonitorWorkerTests.cs`

**Verification:** Search codebase for old namespaces:
```bash
grep -r "GithubActionsAutoscaler.Models" tests/
grep -r "GithubActionsAutoscaler.Services" tests/
```

Should return no results.

---

### Step 11: Verify Complete Solution Build

**Action:** Do a full clean build

```bash
cd c:\dev\psimsa\github-actions-autoscaler
dotnet clean
dotnet build
```

**Expected Result:**
- Solution builds successfully
- No warnings (strict null checking enabled)
- All projects compile

---

### Step 12: Run All Tests

**Action:** Verify all 23 tests still pass with new structure

```bash
cd c:\dev\psimsa\github-actions-autoscaler
dotnet test
```

**Expected Result:**
```
Test summary: total: 23, failed: 0, succeeded: 23, skipped: 0
```

---

### Step 13: Verify Project Structure

**Action:** Confirm final folder structure matches plan

```
src/
├── GithubActionsAutoscaler/                    # Main app (unchanged except removed Models/Services)
├── GithubActionsAutoscaler.Abstractions/       # New - has Models/ and Services/
├── GithubActionsAutoscaler.Queue.Azure/        # New - empty for now
├── GithubActionsAutoscaler.Runner.Docker/      # New - empty for now
```

**Verification Commands:**
```bash
ls -la src/GithubActionsAutoscaler/Models/  # Should be empty or show non-model files
ls -la src/GithubActionsAutoscaler.Abstractions/Models/  # Should have 3 files
ls -la src/GithubActionsAutoscaler.Abstractions/Services/  # Should have 4 files
```

---

### Step 14: Commit Progress

**Action:** Commit the restructure work

```bash
cd c:\dev\psimsa\github-actions-autoscaler
git add .
git commit -m "Phase 1: Create 4-project solution structure with Abstractions project

- Create GithubActionsAutoscaler.Abstractions class library
- Create GithubActionsAutoscaler.Queue.Azure class library
- Create GithubActionsAutoscaler.Runner.Docker class library
- Move Models (Workflow, WorkflowJob, Repository) to Abstractions
- Move shared services (IRepositoryFilter, RepositoryFilter, ILabelMatcher, LabelMatcher) to Abstractions
- Update all imports and namespaces
- Update test project references
- All 23 tests passing with new structure"
```

**Verification:**
```bash
git log --oneline -n 1
```

Should show the commit message.

---

## Verification Checklist

Before considering Phase 1 complete:

- [ ] Solution file includes all 6 projects
- [ ] Main app references Abstractions, Queue.Azure, Runner.Docker
- [ ] Queue.Azure references Abstractions
- [ ] Runner.Docker references Abstractions
- [ ] No circular dependencies
- [ ] Models in Abstractions/Models/ with correct namespace
- [ ] Shared services in Abstractions/Services/ with correct namespace
- [ ] All imports updated in main app and tests
- [ ] Solution builds with `dotnet build` (0 errors, 0 warnings)
- [ ] All 23 tests pass with `dotnet test`
- [ ] Main Models/ and Services/ folders cleaned up (only *.cs files removed, folders may remain empty)
- [ ] Changes committed to feature branch

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

After Phase 1 completion, proceed to **Phase 2: Queue Abstraction** (4-5 hours)

---

*Implementation Plan Version: 1.0*  
*Target Skill Level: Claude Haiku 4.5 / Gemini Flash 3.0*  
*Last Updated: February 1, 2026*
