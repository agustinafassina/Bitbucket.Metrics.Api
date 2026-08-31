using System.Text.Json.Serialization;

namespace Template.Repository.Implementations.ApiModels
{
    internal sealed class CommitUserApi
    {
        [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
        [JsonPropertyName("nickname")] public string? Nickname { get; set; }
        [JsonPropertyName("account_id")] public string? AccountId { get; set; }
        [JsonPropertyName("uuid")] public string? Uuid { get; set; }
    }
}
