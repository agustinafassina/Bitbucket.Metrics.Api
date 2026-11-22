using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Bitbucket.Metrics.Models.Configuration;
using Bitbucket.Metrics.Models.Dto.Bitbucket;
using Bitbucket.Metrics.Repository.Interfaces;

namespace Bitbucket.Metrics.Repository.Implementations
{
    public class CachingBitbucketClient : IBitbucketClient
    {
        private readonly IBitbucketClient _inner;
        private readonly IMemoryCache _cache;
        private readonly RequestCoalescer _coalescer;
        private readonly TimeSpan _ttl;
        private static readonly TimeSpan DiffTtl = TimeSpan.FromHours(6);

        public CachingBitbucketClient(
            IBitbucketClient inner,
            IMemoryCache cache,
            RequestCoalescer coalescer,
            IOptions<BitbucketOptions> options)
        {
            _inner = inner;
            _cache = cache;
            _coalescer = coalescer;
            _ttl = TimeSpan.FromMinutes(options.Value.CacheMinutes > 0 ? options.Value.CacheMinutes : 10);
        }

        // Note: the inner calls receive CancellationToken.None because the fetch is shared between callers;
        // each caller's token only controls how long that caller waits (see RequestCoalescer).

        public Task<IReadOnlyList<BitbucketUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
            => GetOrCreateAsync("bb:users", _ttl, () => _inner.GetUsersAsync(CancellationToken.None), cancellationToken);

        public Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
            => GetOrCreateAsync("bb:repos", _ttl, () => _inner.GetRepositoriesAsync(CancellationToken.None), cancellationToken);

        public Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
            => GetOrCreateAsync(
                $"bb:commits:{repoSlug}:{SinceKey(since)}",
                _ttl,
                () => _inner.GetCommitsAsync(repoSlug, since, CancellationToken.None),
                cancellationToken);

        public Task<IReadOnlyList<BitbucketPullRequestDto>> GetPullRequestsAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
            => GetOrCreateAsync(
                $"bb:prs:{repoSlug}:{StatesKey(states)}:{SinceKey(since)}",
                _ttl,
                () => _inner.GetPullRequestsAsync(repoSlug, states, since, CancellationToken.None),
                cancellationToken);

        public Task<IReadOnlyList<ReviewerMetricDto>> GetReviewerStatsAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
            => GetOrCreateAsync(
                $"bb:reviewers:{repoSlug}:{StatesKey(states)}:{SinceKey(since)}",
                _ttl,
                () => _inner.GetReviewerStatsAsync(repoSlug, states, since, CancellationToken.None),
                cancellationToken);

        public Task<(int LinesAdded, int LinesRemoved)> GetCommitDiffStatAsync(
            string repoSlug,
            string commitHash,
            CancellationToken cancellationToken = default)
            => GetOrCreateAsync(
                $"bb:diff:{repoSlug}:{commitHash}",
                DiffTtl,
                () => _inner.GetCommitDiffStatAsync(repoSlug, commitHash, CancellationToken.None),
                cancellationToken);

        private Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory, CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(key, out T? cached) && cached is not null)
                return Task.FromResult(cached);

            return _coalescer.RunAsync(key, async () =>
            {
                // Another caller may have filled the cache between our miss and joining the in-flight call.
                if (_cache.TryGetValue(key, out T? fresh) && fresh is not null)
                    return fresh;

                T value = await factory().ConfigureAwait(false);
                _cache.Set(key, value, ttl);
                return value;
            }, cancellationToken);
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