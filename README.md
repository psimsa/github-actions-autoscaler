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

The solution operates in three modes:

1. **Webhook Mode** (`App:Mode=Webhook`)
   - Accepts webhook calls from GitHub when a job is queued
   - Places jobs into a queue provider

2. **QueueMonitor Mode** (`App:Mode=QueueMonitor`)
   - Monitors the queue for workflow events
   - Spawns ephemeral Docker containers to execute jobs
   - Removes containers after job completion

3. **Both Mode** (`App:Mode=Both`)
   - Combined webhook + queue monitor for simple deployments

## Requirements

- .NET 10.0 Runtime or Docker
- Docker daemon access (for spawning runner containers)
- Azure Storage Queue (for job coordination)
- GitHub Personal Access Token (for runner registration)

## Project Structure

```
github-actions-autoscaler/
├── src/
│   ├── GithubActionsAutoscaler/               # Main application
│   ├── GithubActionsAutoscaler.Abstractions/  # Shared contracts
│   ├── GithubActionsAutoscaler.Queue.Azure/   # Azure queue provider
│   └── GithubActionsAutoscaler.Runner.Docker/ # Docker runner provider
├── tests/
│   └── GithubActionsAutoscaler.Tests.Unit/    # Unit tests
├── docs/                                      # Documentation
└── Dockerfile                                 # Production Docker image
```

## Configuration

Configuration uses a hierarchical structure in `appsettings.json` or environment variables.

### Example Configuration

```json
{
  "App": {
    "Mode": "Both",
    "GithubToken": "ghp_...",
    "RepositoryFilter": {
      "AllowlistPrefix": "myorg/"
    },
    "Labels": ["self-hosted", "linux", "x64"],
    "OpenTelemetry": {
      "Enabled": true,
      "ServiceName": "github-actions-autoscaler",
      "OtlpEndpoint": "http://localhost:4317"
    }
  },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
      "QueueName": "workflow-job-queued"
    }
  },
  "Runner": {
    "Provider": "Docker",
    "MaxRunners": 3,
    "Docker": {
      "Host": "unix:/var/run/docker.sock",
      "Image": "myoung34/github-runner:latest",
      "RegistryToken": ""
    }
  }
}
```

### OpenTelemetry Configuration

Use either environment variables or `App:OpenTelemetry` config:

- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_SERVICE_NAME`

Environment variables take precedence.

## Quick Start

### Using Docker

```bash
docker run -d \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e App__Mode=Both \
  -e App__GithubToken="ghp_..." \
  -e Queue__Provider=AzureStorageQueue \
  -e Queue__AzureStorageQueue__ConnectionString="your-connection-string" \
  -e Queue__AzureStorageQueue__QueueName="workflow-job-queued" \
  -e Runner__Provider=Docker \
  -e Runner__Docker__Host="unix:/var/run/docker.sock" \
  -e Runner__Docker__Image="myoung34/github-runner:latest" \
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
