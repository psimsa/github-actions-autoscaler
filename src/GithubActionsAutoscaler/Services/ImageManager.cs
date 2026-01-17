using Docker.DotNet;
using Docker.DotNet.Models;
using GithubActionsAutoscaler.Configuration;

namespace GithubActionsAutoscaler.Services;

public class ImageManager : IImageManager
{
    private readonly DockerClient _client;
    private readonly ILogger<ImageManager> _logger;
    private readonly string _dockerToken;
    private readonly bool _autoCheckForImageUpdates;
    private DateTime _lastPullCheck = DateTime.MinValue;

    public ImageManager(
        DockerClient client,
        AppConfiguration configuration,
        ILogger<ImageManager> logger
    )
    {
        _client = client;
        _logger = logger;
        _dockerToken = configuration.DockerToken;
        _autoCheckForImageUpdates = configuration.AutoCheckForImageUpdates;
    }

    public async Task<bool> EnsureImageExistsAsync(string imageName, CancellationToken token)
    {
        if (!_autoCheckForImageUpdates)
        {
            _logger.LogInformation("Auto download of builder image disabled, skipping...");
            return true;
        }

        var success = true;

        var imagesListResponses = await _client.Images.ListImagesAsync(
            new ImagesListParameters() { All = true },
            token
        );
        var tags = imagesListResponses
            .Where(_ => _.RepoTags is { Count: > 0 })
            .SelectMany(_ => _.RepoTags);

        if (tags.Any(_ => _.Equals(imageName)) && _lastPullCheck.AddHours(1) > DateTime.UtcNow)
        {
            return success;
        }

        _logger.LogInformation("Checking for latest image");

        _lastPullCheck = DateTime.UtcNow;
        var m = new ManualResetEventSlim();

        var imageFields = imageName.Split(':');
        var t = Task.Run(
            async () =>
                await _client.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = imageFields[0],
                        Tag = imageFields.Length == 2 ? imageFields[1] : "latest",
                    },
                    new AuthConfig() { Password = _dockerToken },
                    new Progress<JSONMessage>(message =>
                    {
                        if (message.Status.StartsWith("Status:"))
                        {
                            m.Set();
                        }
                    }),
                    CancellationToken.None
                ),
            token
        );

        WaitHandle.WaitAny([m.WaitHandle, token.WaitHandle]);

        if (token.IsCancellationRequested)
            return false;

        _logger.LogInformation("Downloaded new docker image");
        return success;
    }
}
