using System.Text.Json.Serialization;

namespace Autoscaler.Domain.Models;

[JsonSerializable(typeof(Workflow))]
public partial class ApplicationJsonSerializerContext : JsonSerializerContext
{
}