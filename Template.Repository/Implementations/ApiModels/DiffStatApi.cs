using System.Text.Json.Serialization;

namespace Template.Repository.Implementations.ApiModels
{
    internal sealed class DiffStatApi
    {
        [JsonPropertyName("lines_added")] public int LinesAdded { get; set; }
        [JsonPropertyName("lines_removed")] public int LinesRemoved { get; set; }
    }
}
