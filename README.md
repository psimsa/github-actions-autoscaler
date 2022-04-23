# Github Runner Docker Autoscaler

[![Build docker file](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/docker-image.yml/badge.svg)](https://github.com/ofcoursedude/github-actions-autoscaler/actions/workflows/docker-image.yml)

This is a simple autoscaler for Github Actions Runner Docker containers. It also allows for a single instance to be shared across multiple projects.

It uses the myoung34/github-runner  image for running each job as an ephemeral container.
http://github.com/myoung34/github-runner
To run, you need the following:
- Expose a public endpoint for receiving a webhook, or an Azure Storage queue + a way of filling it with Github workflow webhook events (a sample Azure function is included in this repo)
- Start the container with 

Configuration options:


| **Key** | **Default** | **Description** |
| :--- | :--- | :--- |
| **min_containers** | `1` | Minimum number of containers to keep running |