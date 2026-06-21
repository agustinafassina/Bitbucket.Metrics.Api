using System.Text.Json.Serialization;

namespace Template.Repository.Implementations.ApiModels
{
    internal sealed class PagedResponse<T>
    {
        [JsonPropertyName("values")] public List<T>? Values { get; set; }
        [JsonPropertyName("next")] public string? Next { get; set; }
    }
}
