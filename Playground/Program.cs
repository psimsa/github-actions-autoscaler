// See https://aka.ms/new-console-template for more information

using Docker.DotNet;
using Docker.DotNet.Models;

var _client = new DockerClientConfiguration().CreateClient();

var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters()
{
    Filters = new Dictionary<string, IDictionary<string, bool>>()
    {
        {
            "label", new Dictionary<string, bool>()
            {
                { "autoscaler=true", true }
            }
        }
    }
});

Console.WriteLine($"{containers.Count} containers found");
var container = containers.FirstOrDefault();
if(container==null)Environment.Exit(0);

Console.WriteLine(container.Image);
