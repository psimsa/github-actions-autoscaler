# Github Runner Docker Autoscaler

[![Build Multiscaler - docker BuildX](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml)
[![Build and deploy Azure Function](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/WorkflowFunctions.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/WorkflowFunctions.yml)
[![Build full solution](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/BranchBuild.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/BranchBuild.yml)

## Overview

The Github Runner Docker Autoscaler is a solution for automatically scaling GitHub Actions runners using Docker containers. It allows for efficient management of ephemeral runners that can be shared across multiple projects.

It uses the [myoung34/github-runner](https://github.com/myoung34/docker-github-actions-runner) image for running each job as an ephemeral container.

## Getting Started

To run the autoscaler, you need to:

1. **Expose a public endpoint** for receiving webhooks, or set up an Azure Storage queue with a way to fill it with GitHub workflow webhook events (a sample Azure function is included in this repository)
2. **Start the container** with appropriate configuration, ensuring the container has network access and the Docker endpoint is properly configured



## Configuration

Configuration can be done either as environment variables or via a `application.custom.json` JSON file.




### Configuration Options



| **Key**                                 | **Default**                 | **Description**                                            |
|:----------------------------------------|:----------------------------|:-----------------------------------------------------------|
| UseWebEndpoint                          | false                       | Use a web endpoint to receive webhook events.              |
| AzureStorage                            |                             | Azure Storage connection string                            |
| AzureStorageQueue                       |                             | Azure Storage Queue name                                   |
| DockerToken                             |                             | PAT for Docker hub (to avoid daily limits)                 |
| GithubToken                             |                             | PAT for GitHub (to register runners)                       |
| MaxRunners                              | 4                           | Max number of concurrent runners                           |
| RepoWhitelistPrefix                     |                             | Whitelist prefix for GitHub repos                         |
| RepoWhitelist                           |                             | Comma-separated list of whitelisted GitHub repos           |
| IsRepoWhitelistExactMatch               | true                        | Whether items in whitelist are exact matches or prefixes   |
| RepoBlacklistPrefix                     |                             | Blacklist prefix for GitHub repos                          |
| RepoBlacklist                           |                             | Comma-separated list of blacklisted GitHub repos           |
| IsRepoBlacklistExactMatch               | false                       | Whether items in blacklist are exact matches or prefixes   |
| DockerHost                              | unix:/var/run/docker.sock   | Docker endpoint the autoscaler should use                  |
| Labels                                  | self-hosted,[host-arch]     | Comma-separated list of labels applied to runners          |
| ApplicationInsightsConnectionString     |                             | Connection string for Application Insights                 | 




### Configuration Example


```json
{
  "AzureStorage": "DefaultEndpointsProtocol=htt.......",
  "AzureStorageQueue": "workflow-job-queued",
  "DockerHost": "tcp://localhost:2375",
  "DockerToken": "99e16562.......",
  "GithubToken": "ghp_.......",
  "IsRepoWhitelistExactMatch": true,
  "MaxRunners": 3,
  "RepoWhitelist": "",
  "RepoWhitelistPrefix": "ofcoursedude/",
  "UseWebEndpoint": true,
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=dbca7bfd-......."
}
```
