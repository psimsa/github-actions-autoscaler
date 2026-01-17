# GitHub Actions Runner Autoscaler

[![Build Multiscaler - docker BuildX](https://github.com/psimsa/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml/badge.svg)](https://github.com/psimsa/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml)
[![Build full solution](https://github.com/psimsa/github-actions-autoscaler/actions/workflows/BranchBuild.yml/badge.svg)](https://github.com/psimsa/github-actions-autoscaler/actions/workflows/BranchBuild.yml)

A self-hosted GitHub Actions runner autoscaler that dynamically creates ephemeral Docker containers to handle workflow jobs. Designed for personal repositories since GitHub only provides group runners for organizations.

## Features

- **Dynamic Scaling**: Automatically spawns runner containers when jobs are queued
- **Ephemeral Runners**: Each job gets a fresh container that's removed after completion
- **Multi-Server Support**: Run coordinators on multiple servers for distributed scaling
- **Repository Filtering**: Allowlist/denylist repositories by name or prefix
- **Label Matching**: Route jobs to appropriate runners based on labels

## Architecture

The solution operates in two modes:

1. **Web Endpoint Mode** (`UseWebEndpoint=true`)
   - Accepts webhook calls from GitHub when a job is queued
   - Places jobs into an Azure Storage Queue

2. **Runner Coordinator Mode** (`AzureStorage` configured)
   - Monitors Azure Storage Queue for workflow events
   - Spawns ephemeral Docker containers to execute jobs
   - Removes containers after job completion

## Requirements

- .NET 10.0 Runtime or Docker
- Docker daemon access (for spawning runner containers)
- Azure Storage Queue (for job coordination)
- GitHub Personal Access Token (for runner registration)

## Project Structure

```
github-actions-autoscaler/
├── src/
│   └── GithubActionsAutoscaler/     # Main application
│       ├── Configuration/           # App configuration
│       ├── Endpoints/               # REST API endpoints
│       ├── Models/                  # Data models
│       ├── Services/                # Business logic
│       │   ├── DockerService.cs     # Workflow orchestration
│       │   ├── ContainerManager.cs  # Docker container management
│       │   ├── ImageManager.cs      # Docker image management
│       │   ├── RepositoryFilter.cs  # Filtering logic
│       │   └── LabelMatcher.cs      # Label matching logic
│       └── Workers/                 # Background services
├── tests/
│   └── GithubActionsAutoscaler.Tests.Unit/ # Unit tests
├── docs/                            # Documentation
└── Dockerfile                       # Production Docker image
```

## Configuration

Configuration can be done via environment variables or `appsettings.custom.json`:

| Key | Default | Description |
|:----|:--------|:------------|
| `UseWebEndpoint` | `false` | Enable web endpoint for receiving webhooks |
| `AzureStorage` | | Azure Storage connection string |
| `AzureStorageQueue` | | Azure Storage Queue name |
| `DockerToken` | | Docker Hub PAT (to avoid rate limits) |
| `GithubToken` | | GitHub PAT (for runner registration) |
| `MaxRunners` | `4` | Maximum concurrent runners |
| `RepoAllowlistPrefix` | | Prefix for allowed repositories |
| `RepoAllowlist` | | Comma-separated list of allowed repos |
| `IsRepoAllowlistExactMatch` | `true` | Allowlist uses exact matching |
| `RepoDenylistPrefix` | | Prefix for blocked repositories |
| `RepoDenylist` | | Comma-separated list of blocked repos |
| `IsRepoDenylistExactMatch` | `false` | Denylist uses exact matching |
| `DockerHost` | `unix:/var/run/docker.sock` | Docker daemon endpoint |
| `Labels` | `self-hosted,[arch]` | Runner labels |
| `DockerImage` | `myoung34/github-runner:latest` | Runner container image |
| `AutoCheckForImageUpdates` | `true` | Auto-pull latest runner image |
| `CoordinatorHostname` | System hostname | Coordinator instance hostname |

### OpenTelemetry Configuration

Observability is provided via OpenTelemetry with OTLP exporter. Configure using standard OpenTelemetry environment variables or the `appsettings.json` section:

| Key | Default | Description |
|:----|:--------|:------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | | OTLP exporter endpoint (e.g., `http://localhost:4317`) |
| `OTEL_SERVICE_NAME` | `github-actions-autoscaler` | Service name for telemetry |

Alternatively, configure in `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "ServiceName": "github-actions-autoscaler",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

Environment variables take precedence over configuration file settings.

### Example Configuration

```json
{
  "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...",
  "AzureStorageQueue": "workflow-job-queued",
  "DockerHost": "tcp://localhost:2375",
  "GithubToken": "ghp_...",
  "MaxRunners": 3,
  "RepoAllowlistPrefix": "myorg/",
  "UseWebEndpoint": true
}
```

## Quick Start

### Using Docker

```bash
docker run -d \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e AzureStorage="your-connection-string" \
  -e AzureStorageQueue="workflow-job-queued" \
  -e GithubToken="ghp_..." \
  -e RepoAllowlistPrefix="yourusername/" \
  ofcoursedude/github-actions-runner:latest
```

### Using Docker Compose

Create a `docker-compose.yml` file with appropriate environment variables.

### Building from Source

```bash
# Build
dotnet build src/GithubActionsAutoscaler

# Run
dotnet run --project src/GithubActionsAutoscaler
```

## Development

### Prerequisites

- .NET 10.0 SDK
- Docker (for testing runner functionality)

### Build Commands

```bash
# Build solution
dotnet build

# Run unit tests
dotnet test

# Run in development mode
dotnet run --project src/GithubActionsAutoscaler

# Build Docker image
docker build -t github-actions-autoscaler .
```

## License

MIT
