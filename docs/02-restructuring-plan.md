# Restructuring Plan - Modernization to .NET 10 / C# 14

## Phase 1: Repository Structure Modernization

### 1.1 Create New Folder Structure
```
github-actions-autoscaler/
├── src/                           # Source code
│   └── GithubActionsAutoscaler/   # Renamed main project
│       ├── Configuration/         # Configuration classes
│       ├── Endpoints/             # API endpoint definitions
│       ├── Models/                # Data models
│       ├── Services/              # Business logic
│       ├── Workers/               # Background services
│       └── Program.cs             # Entry point
├── tests/                         # Test projects
│   ├── GithubActionsAutoscaler.Tests.Unit/
│   └── GithubActionsAutoscaler.Tests.Integration/
├── docs/                          # Documentation
├── README.md
└── GithubActionsAutoscaler.sln    # Renamed solution file
```

### 1.2 Project Rename
- `AutoscalerApi` → `GithubActionsAutoscaler` (more descriptive name)
- Update all namespace references
- Update solution file

### 1.3 File Moves
| Current Location | New Location |
|-----------------|--------------|
| `AutoscalerApi/` | `src/GithubActionsAutoscaler/` |
| `AutoscalerApi/AppConfiguration.cs` | `src/GithubActionsAutoscaler/Configuration/AppConfiguration.cs` |
| `AutoscalerApi/EndpointRouteBuilderExtensions.cs` | `src/GithubActionsAutoscaler/Endpoints/WorkflowEndpoints.cs` |

## Phase 2: .NET 10 / C# 14 Upgrade

### 2.1 SDK Update
- Update `.csproj` TargetFramework to `net10.0`
- Update LangVersion to `14` (or latest)
- Update NuGet packages to .NET 10 compatible versions

### 2.2 Dockerfile Updates
- Update base images from `8.0` to `10.0`
- Update SDK images for build stage

### 2.3 GitHub Workflows Update
- Update `dotnet-version` from `8.0.x` to `10.0.x`

## Phase 3: Package Updates

| Package | Current | Target |
|---------|---------|--------|
| Azure.Storage.Queues | 12.25.0 | Latest .NET 10 compatible |
| Docker.DotNet | 3.125.15 | Latest |
| Microsoft.ApplicationInsights.WorkerService | 2.23.0 | Latest |
| Microsoft.AspNetCore.OpenApi | 8.0.23 | 10.0.x |
| Swashbuckle.AspNetCore | 6.9.0 | Latest |

## Phase 4: Code Modernization (C# 14 Features)

### 4.1 Language Features to Adopt
- Primary constructors where applicable
- Collection expressions
- File-scoped types where appropriate
- Enhanced pattern matching
- Required members for configuration classes

### 4.2 Records Modernization
- Update records to use positional syntax with `init` properties
- Add null validation

## Phase 5: Update References

### 5.1 Solution File Updates
- Update project paths
- Update solution items paths

### 5.2 Build Scripts and CI/CD
- Update paths in workflows
- Update Dockerfile COPY paths

## Execution Order

1. Create new branch `modernize/dotnet10`
2. Create new folder structure (src/, tests/)
3. Move files to new locations
4. Rename project and namespaces
5. Update solution file
6. Upgrade to .NET 10
7. Update packages
8. Update Dockerfiles
9. Update GitHub workflows
10. Apply C# 14 features
11. Verify build and functionality
12. Commit with detailed message

## Risk Mitigation

- Each step should result in a compilable solution
- Commit after each major phase
- Keep old files until verification complete
- Test Docker build after changes

## Rollback Plan

- Branch can be abandoned if issues arise
- Main branch remains unchanged
- No upstream pushes until fully verified
