# Github Runner Docker Autoscaler

[![Build Multiscaler - docker BuildX](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/MultiArchBuild.yml)
[![Build and deploy Azure Function](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/WorkflowFunctions.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/WorkflowFunctions.yml)
[![Build full solution](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/BranchBuild.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/BranchBuild.yml)

This is a simple autoscaler for Github Actions Runner Docker containers. It also allows for a single instance to be shared across multiple projects.

It uses the [myoung34/github-runner](https://github.com/myoung34/docker-github-actions-runner) image for running each job as an ephemeral container.

To run, you need the following:
- Expose a public endpoint for receiving a webhook, or an Azure Storage queue + a way of filling it with Github workflow webhook events (a sample Azure function is included in this repo)
- Start the container with appropriate configuration

Configuration can be done either as environment variables or as an application.custom.json JSON file.

Configuration options:


| **Key** | **Default** | **Description** |
| :--- | :--- | :--- |
| **AzureStorage** | [empty] | Connection string to Azure Storage, if using a queue |
| **AzureStorageQueue** | [empty] | Name of the queue to use, if using a queue |
| **UseWebEndpoint** | true | Whether to use an endpoint for directly calling a webhook |
| **AzureStorage** | 
| **UseWebEndpoint** | 
| **MaxRunners** | 
| **DockerToken** | 
| **GithubToken** | 
| **RepoPrefix** | 
| **RepoWhitelist** | 
| **IsRepoWhitelistExactMatch** |  