using System.Text.Json.Serialization;

namespace Bitbucket.Metrics.Repository.Implementations.ApiModels
{
    internal sealed class CommitApi
    {
        [JsonPropertyName("hash")] public string? Hash { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("date")] public DateTimeOffset Date { get; set; }
        [JsonPropertyName("author")] public CommitAuthorApi? Author { get; set; }
    }
}
