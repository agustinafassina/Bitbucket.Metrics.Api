using System.Text.Json.Serialization;

namespace Template.Repository.Implementations.ApiModels
{
    internal sealed class WorkspaceMemberApi
    {
        [JsonPropertyName("user")] public ActorApi? User { get; set; }
    }
}
