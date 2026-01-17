namespace GithubActionsAutoscaler.Services;

public interface IImageManager
{
    Task<bool> EnsureImageExistsAsync(string imageName, CancellationToken token);
}
