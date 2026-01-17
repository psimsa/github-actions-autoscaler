# GitHub Actions Runner Autoscaler

[![Build Multiscaler - docker BuildX](https://github.com/psimsa/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml/badge.svg)](https://github.com/psimsa/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml)
[![Build full solution](https://github.com/psimsa/github-actions-autoscaler/actions/workflows/BranchBuild.yml/badge.svg)](https://github.com/psimsa/github-actions-autoscaler/actions/workflows/BranchBuild.yml)

A self-hosted GitHub Actions runner autoscaler that dynamically creates ephemeral Docker containers to handle workflow jobs. Designed for personal repositories since GitHub only provides group runners for organizations.

## Features

- **Dynamic Scaling**: Automatically spawns runner containers when jobs are queued
- **Ephemeral Runners**: Each job gets a fresh container that's removed after completion
- **Multi-Server Support**: Run coordinators on multiple servers for distributed scaling
- **Repository Filtering**: Whitelist/blacklist repositories by name or prefix
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
│       └── Workers/                 # Background services
├── tests/                           # Test projects (coming soon)
├── docs/                            # Documentation
├── samples/                         # Sample deployment configs
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
| `RepoWhitelistPrefix` | | Prefix for allowed repositories |
| `RepoWhitelist` | | Comma-separated list of allowed repos |
| `IsRepoWhitelistExactMatch` | `true` | Whitelist uses exact matching |
| `RepoBlacklistPrefix` | | Prefix for blocked repositories |
| `RepoBlacklist` | | Comma-separated list of blocked repos |
| `IsRepoBlacklistExactMatch` | `false` | Blacklist uses exact matching |
| `DockerHost` | `unix:/var/run/docker.sock` | Docker daemon endpoint |
| `Labels` | `self-hosted,[arch]` | Runner labels |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | | Application Insights connection |
| `DockerImage` | `myoung34/github-runner:latest` | Runner container image |
| `AutoCheckForImageUpdates` | `true` | Auto-pull latest runner image |

### Example Configuration

```json
{
  "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...",
  "AzureStorageQueue": "workflow-job-queued",
  "DockerHost": "tcp://localhost:2375",
  "GithubToken": "ghp_...",
  "MaxRunners": 3,
  "RepoWhitelistPrefix": "myorg/",
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
  -e RepoWhitelistPrefix="yourusername/" \
  ofcoursedude/github-actions-runner:latest
```

### Using Docker Compose

See `samples/docker-compose.yml` for a complete example.

### Building from Source

```bash
# Build
dotnet build src/GithubActionsAutoscaler

# Run
dotnet run --project src/GithubActionsAutoscaler
```

## Development

### Prerequisites

- .NET 10.0 SDK (preview)
- Docker (for testing runner functionality)

### Build Commands

```bash
# Build solution
dotnet build

# Run in development mode
dotnet run --project src/GithubActionsAutoscaler

# Build Docker image
docker build -t github-actions-autoscaler .
```

## License

MIT
