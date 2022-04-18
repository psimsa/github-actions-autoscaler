# Github Runner Docker Autoscaler

[![Build Multiscaler - docker BuildX](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml)
[![Build and deploy Azure Function](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/WorkflowFunctions.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/WorkflowFunctions.yml)
[![Build full solution](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/BranchBuild.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/BranchBuild.yml)

This is a simple autoscaler for Github Actions Runner Docker containers. It also allows for a single instance to be shared across multiple projects.

It uses the [myoung34/github-runner](https://github.com/myoung34/docker-github-actions-runner) image for running each job as an ephemeral container.

To run, you need the following:
- Expose a public endpoint for receiving a webhook, or an Azure Storage queue + a way of filling it with Github workflow webhook events (a sample Azure function is included in this repo)
- Start the container with appropriate configuration (e.g. make sure the container has network access and configured docker endpoint and bind)

Configuration can be done either as environment variables or as an application.custom.json JSON file.

Configuration options:

| **Key**                                 | **Default**                 | **Description**                                            |
|:----------------------------------------|:----------------------------|:-----------------------------------------------------------|
| UseWebEndpoint                          | false                       | Use a web endpoint to receive webhook events.              |
| AzureStorage                            |                             | Azure Storage connection string                            |
| AzureStorageQueue                       |                             | Azure Storage Queue name                                   |
| DockerToken                             |                             | PAT for Docker hub (to avoid daily limits)                 |
| GithubToken                             |                             | PAT for GitHub.com (to register runners)                   |
| MaxRunners                              | 4                           | Max number of concurrent runners                           |
| RepoWhitelistPrefix                     |                             | Whitelist prefix for github repos                          |
| RepoWhitelist                           |                             | Comma-separated list of whitelisted github repos           |
| IsRepoWhitelistExactMatch               | true                        | Whether items in whitelist are exact matches or prefixes   |
| RepoBlacklistPrefix                     |                             | Blacklist prefix for github repos                          |
| RepoBlacklist                           |                             | Comma-separated list of blacklisted github repos           |
| IsRepoBlacklistExactMatch               | false                       | Whether items in blacklisted are exact matches or prefixes |
| DockerHost                              | unix:/var/run/docker.sock   | Docker endpoint the autoscaler should use                  |
| Labels                                  | self-hosted,[host-arch]     | Comma-separated list of labels applied to runners          |
| ApplicationInsightsConnectionString     |                             | Connection string for Application Insights                 | 

Configuration sample:
```json
{
  "AzureStorage" :"DefaultEndpointsProtocol=htt.......",
  "AzureStorageQueue" : "workflow-job-queued",
  "DockerHost" : "tcp://localhost :2375",
  "DockerToken" : "99e16562.......",
  "GithubToken" : "ghp_.......",
  "IsRepoWhitelistExactMatch" : true,
  "MaxRunners" : 3,
  "RepoWhitelist" : "",
  "RepoWhitelistPrefix" : "ofcoursedude/",
  "UseWebEndpoint" : true,
  "APPLICATIONINSIGHTS_CONNECTION_STRING" :"InstrumentationKey=dbca7bfd-......."
}
```
