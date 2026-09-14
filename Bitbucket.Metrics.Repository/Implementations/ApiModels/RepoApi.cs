using System.Text.Json.Serialization;

namespace Bitbucket.Metrics.Repository.Implementations.ApiModels
{
    internal sealed class RepoApi
    {
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("full_name")] public string? FullName { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("updated_on")] public DateTimeOffset? UpdatedOn { get; set; }
    }
}
