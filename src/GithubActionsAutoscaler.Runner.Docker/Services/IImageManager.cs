namespace GithubActionsAutoscaler.Runner.Docker.Services;

public interface IImageManager
{
    Task<bool> EnsureImageExistsAsync(string imageName, CancellationToken token);
}
