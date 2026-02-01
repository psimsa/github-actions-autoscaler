# Migration Guide: v1.x to v2.0

## Date: February 1, 2026

## Overview

Version 2.0 is a major restructuring of the GitHub Actions Autoscaler with:
- Enhanced provider abstraction for queue and runner management
- Hierarchical configuration structure
- Explicit Mode-based operation (Webhook, QueueMonitor, Both)
- All configuration defaults moved to appsettings.json
- Improved testability and extensibility

This guide helps you migrate from v1.x to v2.0.

---

## Key Changes Summary

### 1. Configuration Model

#### v1.x (Flat)
```json
{
  "AzureStorage": "DefaultEndpointsProtocol=https;...",
  "AzureStorageQueue": "workflow-jobs",
  "DockerHost": "unix:/var/run/docker.sock",
  "DockerImage": "myoung34/github-runner:latest",
  "GithubToken": "ghp_...",
  "MaxRunners": 4,
  "RepoAllowlist": "org1/*,org2/specific-repo",
  "DockerToken": "dckr_...",
  "Labels": "self-hosted,linux,x64"
}
```

#### v2.0 (Hierarchical)
```json
{
  "App": {
    "Mode": "Both",
    "GithubToken": "ghp_...",
    "RepositoryFilter": {
      "AllowlistPrefix": "org1/",
      "Allowlist": ["org1/special-repo"],
      "IsAllowlistExactMatch": false
    },
    "Labels": ["self-hosted", "linux", "x64"]
  },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": {
      "ConnectionString": "DefaultEndpointsProtocol=https;...",
      "QueueName": "workflow-jobs"
    }
  },
  "Runner": {
    "Provider": "Docker",
    "MaxRunners": 4,
    "Docker": {
      "Host": "unix:/var/run/docker.sock",
      "Image": "myoung34/github-runner:latest",
      "RegistryToken": "dckr_..."
    }
  }
}
```

### 2. Application Mode

**New Concept:** The application can run in three modes:

| Mode | Webhook | Queue Monitor | Use Case |
|------|---------|---------------|----------|
| `Webhook` | ✅ | ❌ | Webhook receiver only (enqueues jobs) |
| `QueueMonitor` | ❌ | ✅ | Runner coordinator only (consumes jobs) |
| `Both` | ✅ | ✅ | All-in-one (development, simple deployments) |

**v1.x Behavior:** Implicit based on configuration presence
- `UseWebEndpoint=true` → Webhook mode
- `AzureStorage` configured → QueueMonitor mode

**v2.0 Behavior:** Explicit via `App:Mode` configuration

### 3. Docker Compose Examples

#### v2.0 - Development (All-in-One)
```yaml
services:
  autoscaler:
    image: ghcr.io/owner/autoscaler:v2.0
    ports:
      - "8080:8080"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - App__Mode=Both
      - App__GithubToken=${GITHUB_TOKEN}
      - Queue__Provider=AzureStorageQueue
      - Queue__AzureStorageQueue__ConnectionString=${AZURE_STORAGE_CONN}
      - Runner__Provider=Docker
      - Runner__MaxRunners=4
```

#### v2.0 - Production (Distributed)
```yaml
services:
  webhook:
    image: ghcr.io/owner/autoscaler:v2.0
    ports:
      - "8080:8080"
    environment:
      - App__Mode=Webhook
      - Queue__Provider=AzureStorageQueue
      - Queue__AzureStorageQueue__ConnectionString=${AZURE_STORAGE_CONN}

  monitor-host1:
    image: ghcr.io/owner/autoscaler:v2.0
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - App__Mode=QueueMonitor
      - App__GithubToken=${GITHUB_TOKEN}
      - Queue__Provider=AzureStorageQueue
      - Queue__AzureStorageQueue__ConnectionString=${AZURE_STORAGE_CONN}
      - Runner__Provider=Docker
      - Runner__MaxRunners=4
```

---

## Migration Checklist

### Step 1: Backup Current Configuration

Save your current `appsettings.json` and `appsettings.custom.json` (if present):

```powershell
# PowerShell
Copy-Item appsettings.json appsettings.json.v1.backup
Copy-Item appsettings.custom.json appsettings.custom.json.v1.backup -ErrorAction SilentlyContinue
```

### Step 2: Update Application

```bash
# Pull v2.0 image (or build from source on feature/v2-restructure branch)
docker pull ghcr.io/owner/autoscaler:v2.0
```

### Step 3: Migrate Configuration

Create a new `appsettings.json` using the v2.0 structure. Map your v1.x settings:

**Mapping Reference:**

| v1.x Setting | v2.0 Path | Notes |
|--------------|-----------|-------|
| `UseWebEndpoint` | `App:Mode` | Set to `Webhook`, `QueueMonitor`, or `Both` |
| `GithubToken` | `App:GithubToken` | Moved to App section |
| `AzureStorage` | `Queue:AzureStorageQueue:ConnectionString` | Renamed and reorganized |
| `AzureStorageQueue` | `Queue:AzureStorageQueue:QueueName` | Moved under Queue provider |
| `DockerHost` | `Runner:Docker:Host` | Moved under Runner provider |
| `DockerImage` | `Runner:Docker:Image` | Moved under Runner provider |
| `DockerToken` | `Runner:Docker:RegistryToken` | Renamed and moved |
| `MaxRunners` | `Runner:MaxRunners` | Moved to Runner section |
| `Labels` | `App:Labels` | Array instead of comma-separated string |
| `RepoAllowlist` | `App:RepositoryFilter:Allowlist` | New structure for filtering |
| `RepoAllowlistPrefix` | `App:RepositoryFilter:AllowlistPrefix` | New structure |
| `RepoDenylist` | `App:RepositoryFilter:Denylist` | New structure |
| `RepoDenylistPrefix` | `App:RepositoryFilter:DenylistPrefix` | New structure |

### Step 4: Test Configuration

Run the application with `--validate-config` flag (optional feature):

```bash
dotnet GithubActionsAutoscaler.dll --validate-config
```

When missing required settings, you'll see clear error messages at startup.

### Step 5: Deploy and Monitor

After successful startup with new configuration, monitor logs for any issues:

```bash
docker logs <container-id> -f
```

---

## Common Migration Scenarios

### Scenario 1: Single-Host All-in-One Deployment

**Before (v1.x):**
```json
{
  "UseWebEndpoint": true,
  "AzureStorage": "DefaultEndpointsProtocol=https;...",
  "AzureStorageQueue": "jobs",
  "GithubToken": "ghp_...",
  "MaxRunners": 4
}
```

**After (v2.0):**
```json
{
  "App": {
    "Mode": "Both",
    "GithubToken": "ghp_..."
  },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": {
      "ConnectionString": "DefaultEndpointsProtocol=https;...",
      "QueueName": "jobs"
    }
  },
  "Runner": {
    "Provider": "Docker",
    "MaxRunners": 4
  }
}
```

### Scenario 2: Webhook-Only Relay

**Before (v1.x):**
```json
{
  "UseWebEndpoint": true,
  "AzureStorage": "DefaultEndpointsProtocol=https;..."
}
```

**After (v2.0):**
```json
{
  "App": {
    "Mode": "Webhook"
  },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": {
      "ConnectionString": "DefaultEndpointsProtocol=https;..."
    }
  }
}
```

**Note:** No `Runner` section needed in Webhook-only mode.

### Scenario 3: Multiple Coordinators

**Before (v1.x):**
Each coordinator instance had:
```json
{
  "AzureStorage": "DefaultEndpointsProtocol=https;...",
  "GithubToken": "ghp_...",
  "MaxRunners": 2
}
```

**After (v2.0):**
Each coordinator instance now has:
```json
{
  "App": {
    "Mode": "QueueMonitor",
    "GithubToken": "ghp_..."
  },
  "Queue": {
    "Provider": "AzureStorageQueue",
    "AzureStorageQueue": {
      "ConnectionString": "DefaultEndpointsProtocol=https;..."
    }
  },
  "Runner": {
    "Provider": "Docker",
    "MaxRunners": 2
  }
}
```

---

## Troubleshooting

### Issue: "Required configuration missing: Queue:AzureStorageQueue:ConnectionString"

**Cause:** Queue connection string not provided

**Solution:** Ensure `Queue:AzureStorageQueue:ConnectionString` is set via:
- `appsettings.json`
- Environment variable: `Queue__AzureStorageQueue__ConnectionString`
- User secrets (development)

### Issue: Application starts in wrong mode

**Cause:** `App:Mode` not explicitly set or incorrect value

**Solution:** Check `App:Mode` is one of: `Webhook`, `QueueMonitor`, `Both`

### Issue: Docker runners not starting

**Cause:** `Runner:MaxRunners` set to 0 or negative

**Solution:** Set `Runner:MaxRunners` to positive integer (default: 4)

### Issue: Labels not recognized

**Cause:** Array format differs from v1.x comma-separated string

**Solution:** Use array format in v2.0:
```json
{
  "App": {
    "Labels": ["self-hosted", "linux", "x64"]
  }
}
```

---

## Rollback Instructions

If you need to rollback to v1.x:

```bash
# Restore v1.x configuration
cp appsettings.json.v1.backup appsettings.json

# Downgrade docker image
docker pull ghcr.io/owner/autoscaler:v1.x
# Or rebuild from v1.x branch
git checkout main
make build
```

---

## Support & Questions

For issues during migration:

1. Check [feasibility-analysis.md](05-feasibility-analysis.md) for architecture details
2. Review [architecture-decision.md](06-application-architecture-decision.md) for design rationale
3. Create an issue on GitHub for bugs or unexpected behavior

---

*Migration Guide Version: 1.0*  
*Last Updated: February 1, 2026*
