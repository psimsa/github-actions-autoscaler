using System.Text.Json.Serialization;

namespace AutoscalerApi.Models;

[JsonSerializable(typeof(Workflow))]
public partial class ApplicationJsonSerializerContext : JsonSerializerContext
{
}