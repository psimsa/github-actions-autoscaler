

# Github Runner Docker Autoscaler Repository Overview

## Purpose
The Github Runner Docker Autoscaler is a solution for automatically scaling GitHub Actions runners using Docker containers. It allows for efficient management of ephemeral runners that can be shared across multiple projects. The autoscaler uses the [myoung34/github-runner](https://github.com/myoung34/docker-github-actions-runner) Docker image to run each job in a separate container.

## General Setup
The repository contains a .NET application that serves as the autoscaler core. It can be configured using environment variables or a JSON configuration file. Key features include:

- **Webhook Support**: Can receive GitHub workflow events via webhooks or Azure Storage queues
- **Multi-architecture Support**: Builds for both x64 and ARM64 architectures
- **Configuration Options**: Supports various configuration options for Docker, GitHub, and Azure integration
- **Scaling Logic**: Automatically scales runners based on demand up to a configurable maximum

## Repository Structure
```
.
├── .config/                  # Configuration files
├── .devcontainer/            # Development container configuration
├── .github/                  # GitHub workflows and CI configuration
│   └── workflows/            # CI/CD pipelines
│       ├── BranchBuild.yml  # Build solution on branch push
│       ├── MultiArchBuild.yml # Multi-architecture Docker builds
│       └── codeql-analysis.yml # Code quality analysis
├── .husky/                  # Git hooks for pre-commit validation
├── .vscode/                 # VSCode configuration
├── AutoscalerApi/           # Main application code
│   ├── Models/               # Data models
│   ├── Properties/           # Assembly info
│   ├── Services/             # Business logic services
│   ├── Workers/              # Background workers
│   ├── AppConfiguration.cs   # Configuration management
│   ├── Dockerfile            # Docker container configuration
│   ├── Program.cs            # Main application entry point
│   └── ...                   # Other core components
├── README.md                # Project documentation
```

## CI/CD Pipelines
The repository includes several GitHub Actions workflows:

1. **BranchBuild.yml**: Builds the solution on every push and creates binary artifacts for the main branch
2. **MultiArchBuild.yml**: Builds and pushes Docker images for multiple architectures (x64, ARM64)
3. **codeql-analysis.yml**: Runs CodeQL analysis for code quality and security

## Development Setup
The project uses Husky for git hooks with C# code formatting via csharpier. The main application is built with .NET and can be run as a Docker container.

## Configuration Options
Key configuration options include:
- `UseWebEndpoint`: Enable webhook endpoint
- `AzureStorage`: Azure Storage connection string
- `AzureStorageQueue`: Queue name for workflow events
- `DockerToken`: Docker Hub PAT
- `GithubToken`: GitHub PAT for runner registration
- `MaxRunners`: Maximum concurrent runners
- `RepoAllowlist/Blocklist`: Repository access control
- `DockerHost`: Docker endpoint configuration
- `Labels`: Labels applied to runners
- `ApplicationInsightsConnectionString`: Telemetry configuration

