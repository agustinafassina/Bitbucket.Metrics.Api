using System.Text.Json.Serialization;

namespace Bitbucket.Metrics.Repository.Implementations.ApiModels
{
    internal sealed class ParticipantApi
    {
        [JsonPropertyName("user")] public ActorApi? User { get; set; }
        [JsonPropertyName("role")] public string? Role { get; set; }
        [JsonPropertyName("approved")] public bool Approved { get; set; }
    }
}
