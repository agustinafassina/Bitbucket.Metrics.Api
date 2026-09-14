using System.Text.Json.Serialization;

namespace Bitbucket.Metrics.Repository.Implementations.ApiModels
{
    internal sealed class WorkspaceMemberApi
    {
        [JsonPropertyName("user")] public ActorApi? User { get; set; }
    }
}
