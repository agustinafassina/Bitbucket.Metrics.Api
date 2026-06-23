using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Template.Models.Configuration;
using Template.Models.Dto.Bitbucket;
using Template.Repository.Interfaces;

namespace Template.Repository.Implementations
{
    public class CachingBitbucketClient : IBitbucketClient
    {
        private readonly IBitbucketClient _inner;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _ttl;
        private static readonly TimeSpan DiffTtl = TimeSpan.FromHours(6);

        public CachingBitbucketClient(IBitbucketClient inner, IMemoryCache cache, IOptions<BitbucketOptions> options)
        {
            _inner = inner;
            _cache = cache;
            _ttl = TimeSpan.FromMinutes(options.Value.CacheMinutes > 0 ? options.Value.CacheMinutes : 10);
        }

        public Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
            => GetOrCreateAsync("bb:repos", _ttl, () => _inner.GetRepositoriesAsync(cancellationToken));

        public Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
            => GetOrCreateAsync(
                $"bb:commits:{repoSlug}:{SinceKey(since)}",
                _ttl,
                () => _inner.GetCommitsAsync(repoSlug, since, cancellationToken));

        public Task<IReadOnlyList<BitbucketPullRequestDto>> GetPullRequestsAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
            => GetOrCreateAsync(
                $"bb:prs:{repoSlug}:{StatesKey(states)}:{SinceKey(since)}",
                _ttl,
                () => _inner.GetPullRequestsAsync(repoSlug, states, since, cancellationToken));

        public Task<IReadOnlyList<ReviewerMetricDto>> GetReviewerStatsAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
            => GetOrCreateAsync(
                $"bb:reviewers:{repoSlug}:{StatesKey(states)}:{SinceKey(since)}",
                _ttl,
                () => _inner.GetReviewerStatsAsync(repoSlug, states, since, cancellationToken));

        public async Task<(int LinesAdded, int LinesRemoved)> GetCommitDiffStatAsync(
            string repoSlug,
            string commitHash,
            CancellationToken cancellationToken = default)
        {
            string key = $"bb:diff:{repoSlug}:{commitHash}";
            if (_cache.TryGetValue(key, out (int Added, int Removed) cached))
                return cached;

            (int LinesAdded, int LinesRemoved) result = await _inner.GetCommitDiffStatAsync(repoSlug, commitHash, cancellationToken);
            _cache.Set(key, (result.LinesAdded, result.LinesRemoved), DiffTtl);
            return result;
        }

        private async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory)
        {
            if (_cache.TryGetValue(key, out T? cached) && cached is not null)
                return cached;

            T value = await factory();
            _cache.Set(key, value, ttl);
            return value;
        }

        private static string SinceKey(DateTimeOffset? since)
        {
            if (!since.HasValue)
                return "all";
            DateTime u = since.Value.UtcDateTime;
            return new DateTime(u.Year, u.Month, u.Day, u.Hour, 0, 0, DateTimeKind.Utc).ToString("yyyyMMddHH");
        }

        private static string StatesKey(IEnumerable<string> states)
            => states is null ? string.Empty : string.Join(',', states.OrderBy(s => s, StringComparer.Ordinal));
    }
}