using System.Text.Json.Serialization;

namespace Template.Repository.Implementations.ApiModels
{
    internal sealed class PullRequestApi
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("created_on")] public DateTimeOffset CreatedOn { get; set; }
        [JsonPropertyName("updated_on")] public DateTimeOffset UpdatedOn { get; set; }
        [JsonPropertyName("comment_count")] public int CommentCount { get; set; }
        [JsonPropertyName("author")] public ActorApi? Author { get; set; }
        [JsonPropertyName("participants")] public List<ParticipantApi>? Participants { get; set; }
    }
}
