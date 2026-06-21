using System.Text.Json.Serialization;

namespace Template.Repository.Implementations.ApiModels
{
    internal sealed class CommitAuthorApi
    {
        [JsonPropertyName("raw")] public string? Raw { get; set; }
        [JsonPropertyName("user")] public CommitUserApi? User { get; set; }
    }
}
