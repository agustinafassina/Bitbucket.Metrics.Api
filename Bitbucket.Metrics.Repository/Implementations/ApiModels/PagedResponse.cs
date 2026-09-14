using System.Text.Json.Serialization;

namespace Bitbucket.Metrics.Repository.Implementations.ApiModels
{
    internal sealed class PagedResponse<T>
    {
        [JsonPropertyName("values")] public List<T>? Values { get; set; }
        [JsonPropertyName("next")] public string? Next { get; set; }
    }
}
