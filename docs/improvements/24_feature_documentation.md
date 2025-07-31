# Issue: Missing Feature Documentation

## Current Problem
Some features are not documented in the README.

## Recommendation
Update the README to document all features and configuration options.

## Implementation Steps

1. Add a features section to the README:
```markdown
## Features

### Docker Runner Management
- Automatic scaling of GitHub Actions runners
- Support for multiple concurrent runners
- Container lifecycle management
- Automatic cleanup of old containers

### Repository Filtering
- Whitelist/blacklist repositories
- Prefix-based filtering
- Exact match or prefix match options

### Configuration Options
- Environment variables or JSON configuration
- Docker endpoint configuration
- GitHub and Docker authentication
- Custom labels for runners

### Monitoring and Logging
- Application Insights integration
- Detailed logging of operations
- Health check endpoints

### Deployment Options
- Web endpoint for direct webhook reception
- Azure Storage Queue support
- Docker container deployment
```

2. Add configuration examples:
```markdown
## Configuration Examples

### Basic Configuration
```json
{
  "GithubToken": "ghp_...",
  "DockerToken": "dckr_...",
  "MaxRunners": 4,
  "Labels": "self-hosted,linux"
}
```

### Advanced Configuration
```json
{
  "GithubToken": "ghp_...",
  "DockerToken": "dckr_...",
  "MaxRunners": 10,
  "RepoWhitelistPrefix": "myorg/",
  "RepoBlacklist": "myorg/private-repo",
  "Labels": "self-hosted,linux,x64",
  "DockerHost": "tcp://localhost:2375",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=..."
}
```

### Azure Storage Queue Configuration
```json
{
  "GithubToken": "ghp_...",
  "DockerToken": "dckr_...",
  "MaxRunners": 5,
  "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...",
  "AzureStorageQueue": "workflow-jobs",
  "UseWebEndpoint": false
}
```
```

3. Add deployment instructions:
```markdown
## Deployment

### Docker Deployment
```bash
docker run -d \
  -e GithubToken=ghp_... \
  -e DockerToken=dckr_... \
  -e MaxRunners=4 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -p 8080:80 \
  myoung34/github-runner-autoscaler:latest
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: github-runner-autoscaler
spec:
  replicas: 1
  selector:
    matchLabels:
      app: github-runner-autoscaler
  template:
    metadata:
      labels:
        app: github-runner-autoscaler
    spec:
      containers:
      - name: autoscaler
        image: myoung34/github-runner-autoscaler:latest
        env:
        - name: GithubToken
          valueFrom:
            secretKeyRef:
              name: github-token
              key: token
        - name: DockerToken
          valueFrom:
            secretKeyRef:
              name: docker-token
              key: token
        - name: MaxRunners
          value: "4"
        volumeMounts:
        - name: docker-socket
          mountPath: /var/run/docker.sock
      volumes:
      - name: docker-socket
        hostPath:
          path: /var/run/docker.sock
```
```

4. Add troubleshooting section:
```markdown
## Troubleshooting

### Common Issues

1. **Containers not starting**:
   - Check Docker daemon is running
   - Verify Docker socket permissions
   - Check container logs for errors

2. **Runners not registering**:
   - Verify GitHub token has correct permissions
   - Check repository whitelist/blacklist configuration
   - Verify network connectivity to GitHub

3. **Webhook not received**:
   - Check firewall/NAT configuration
   - Verify webhook URL in GitHub settings
   - Check application logs for errors

### Logs

Logs are available in the following locations:
- Docker container logs: `docker logs <container_id>`
- Application logs: `/var/log/autoscaler.log`
- Azure Application Insights (if configured)
```

5. Add contribution guidelines:
```markdown
## Contributing

### Development Setup

1. Clone the repository
2. Install .NET 6 SDK
3. Run `dotnet restore`
4. Run `dotnet build`
5. Run tests with `dotnet test`

### Code Style
n- Follow C# coding conventions
- Use async/await for I/O operations
- Add XML documentation to public methods
- Write unit tests for new features

### Pull Requests

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request
```

## Benefits
- Better documentation of features
- Clearer configuration examples
- Improved user experience
- Easier troubleshooting
- Encourages contributions
