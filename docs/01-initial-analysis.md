# Initial Analysis - GitHub Actions Autoscaler

## Date: January 17, 2026

## Project Overview

This is a .NET 8.0 ASP.NET Core application designed to autoscale self-hosted GitHub Actions runners using Docker containers. The solution targets personal repositories since GitHub only provides group runners for organizations.

## Architecture

### Two Operation Modes

1. **Web Endpoint Mode** (`UseWebEndpoint=true`)
   - Accepts webhook calls from GitHub when a job is queued for a self-hosted runner
   - Places jobs into an Azure Storage Queue
   - Single instance deployment pattern

2. **Runner Coordinator Mode** (`AzureStorage` configured)
   - Monitors Azure Storage Queue for workflow events
   - Spawns ephemeral Docker containers to execute GitHub Actions jobs
   - Removes containers after job completion
   - Can run on multiple servers for distributed scaling

### Current Project Structure

```
github-actions-autoscaler/
├── AutoscalerApi/                 # Main .NET project
│   ├── Models/                    # Data models (Workflow, WorkflowJob, Repository)
│   ├── Services/                  # Business logic (DockerService)
│   ├── Workers/                   # Background services (QueueMonitorWorker)
│   ├── Program.cs                 # Application entry point
│   ├── AppConfiguration.cs        # Configuration management
│   ├── EndpointRouteBuilderExtensions.cs  # API endpoint definitions
│   ├── Dockerfile                 # Multi-arch production image
│   ├── Dockerfile-alpine          # Alpine-based image
│   └── Dockerfile-debug           # Debug image
├── sample-config/                 # Example deployment configurations
├── .github/workflows/             # CI/CD pipelines
├── .devcontainer/                 # VS Code dev container
├── README.md                      # Project documentation
└── github-actions-autoscaler.sln  # Solution file
```

## Key Components Analysis

### 1. AppConfiguration.cs
- Configuration class with all settings
- Factory method `FromConfiguration(IConfiguration)` for building from config sources
- Supports environment variables and JSON configuration
- Key settings: AzureStorage, GithubToken, MaxRunners, repo whitelist/blacklist

### 2. DockerService.cs (371 lines)
- Core business logic for managing Docker containers
- Responsibilities:
  - Creating ephemeral runner containers
  - Managing container lifecycle
  - Pulling Docker images
  - Repository whitelist/blacklist filtering
  - Label matching for job routing
  - Container cleanup (guard task)
- Uses `Docker.DotNet` library for Docker API communication

### 3. QueueMonitorWorker.cs (108 lines)
- Background service implementing `IHostedService`
- Polls Azure Storage Queue for workflow events
- Deserializes Base64-encoded JSON messages
- Delegates processing to `IDockerService`
- Handles retry logic for failed processing

### 4. EndpointRouteBuilderExtensions.cs
- Defines REST API endpoints
- `/workflow/ping` - Health check
- `/workflow/enqueue-job` - Accepts webhook payloads and queues them

### 5. Models
- `Workflow` - Root webhook payload (action, job, repository)
- `WorkflowJob` - Job details (name, labels, run_id)
- `Repository` - Repository info (full_name, name)
- All use `record` types with JSON serialization attributes

## Technical Stack

- **Framework:** .NET 8.0 / ASP.NET Core
- **Language:** C# 12 (implicit with .NET 8)
- **Dependencies:**
  - Azure.Storage.Queues (12.25.0)
  - Docker.DotNet (3.125.15)
  - Microsoft.ApplicationInsights.WorkerService (2.23.0)
  - Microsoft.AspNetCore.OpenApi (8.0.23)
  - Swashbuckle.AspNetCore (6.9.0)

## Issues Identified

### Structural Issues
1. No `src/` folder - project at root level
2. No test project or test infrastructure
3. Single project containing all concerns (no separation of layers)

### Code Quality Issues
1. `DockerService` is too large (371 lines) with multiple responsibilities
2. No proper cancellation token handling in some async methods
3. Container guard task created without proper lifecycle management
4. Hardcoded values scattered throughout (e.g., timeouts)
5. Some null reference potential issues with configuration values
6. No input validation on API endpoints
7. Missing async suffix on some async methods

### Configuration Issues
1. Suppressed trimming warnings without proper handling
2. Configuration null handling could cause runtime errors

### Documentation Issues
1. README references outdated badge links (ofcoursedude vs psimsa)
2. AGENTS.md already exists with good guidelines
3. No architectural documentation

### Security Concerns
1. `sample-config/docker-compose.yml` contains actual tokens/secrets (should be examples only)

## Modernization Requirements

Per abstract.md, the modernization should:
1. Upgrade to .NET 10 / C# 14
2. Restructure to include `src/` and `tests/` folders
3. Follow modern best practices
4. Add comprehensive test coverage
5. Improve componentization and testability
