using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Bitbucket.Metrics.Models.Configuration;
using Bitbucket.Metrics.Models.Dto.Bitbucket;
using Bitbucket.Metrics.Repository.Implementations;
using Bitbucket.Metrics.Repository.Interfaces;

namespace Bitbucket.Metrics.UnitTests.Repository;

public class CachingBitbucketClientTests
{
    private static readonly IReadOnlyList<BitbucketRepositoryDto> Repos =
        new List<BitbucketRepositoryDto> { new() { Slug = "repo1" } };

    private static (CachingBitbucketClient Sut, RequestCoalescer Coalescer) CreateSut(IBitbucketClient inner)
    {
        RequestCoalescer coalescer = new();
        CachingBitbucketClient sut = new(
            inner,
            new MemoryCache(new MemoryCacheOptions()),
            coalescer,
            Options.Create(new BitbucketOptions { CacheMinutes = 10 }));
        return (sut, coalescer);
    }

    [Fact]
    public async Task Concurrent_cache_misses_trigger_a_single_upstream_call()
    {
        TaskCompletionSource<IReadOnlyList<BitbucketRepositoryDto>> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IBitbucketClient> inner = new();
        inner.Setup(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>())).Returns(gate.Task);
        (CachingBitbucketClient sut, _) = CreateSut(inner.Object);

        Task<IReadOnlyList<BitbucketRepositoryDto>>[] callers = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => sut.GetRepositoriesAsync()))
            .ToArray();

        await Task.Delay(50); // let every caller reach the cache miss before the upstream call completes
        gate.SetResult(Repos);
        IReadOnlyList<BitbucketRepositoryDto>[] results = await Task.WhenAll(callers);

        inner.Verify(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.All(results, r => Assert.Same(Repos, r));
    }

    [Fact]
    public async Task Second_call_is_served_from_cache()
    {
        Mock<IBitbucketClient> inner = new();
        inner.Setup(c => c.GetCommitDiffStatAsync("repo1", "abc", It.IsAny<CancellationToken>())).ReturnsAsync((4, 2));
        (CachingBitbucketClient sut, RequestCoalescer coalescer) = CreateSut(inner.Object);

        (int, int) first = await sut.GetCommitDiffStatAsync("repo1", "abc");
        (int, int) second = await sut.GetCommitDiffStatAsync("repo1", "abc");

        Assert.Equal((4, 2), first);
        Assert.Equal(first, second);
        Assert.Equal(0, coalescer.InFlightCount);
        inner.Verify(c => c.GetCommitDiffStatAsync("repo1", "abc", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Failed_call_is_not_cached_and_next_call_retries()
    {
        Mock<IBitbucketClient> inner = new();
        inner.SetupSequence(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
             .ThrowsAsync(new HttpRequestException("boom"))
             .ReturnsAsync(Repos);
        (CachingBitbucketClient sut, RequestCoalescer coalescer) = CreateSut(inner.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetRepositoriesAsync());
        Assert.Equal(0, coalescer.InFlightCount);

        IReadOnlyList<BitbucketRepositoryDto> result = await sut.GetRepositoriesAsync();

        Assert.Same(Repos, result);
        inner.Verify(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task One_caller_cancelling_does_not_fail_the_others()
    {
        TaskCompletionSource<IReadOnlyList<BitbucketRepositoryDto>> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IBitbucketClient> inner = new();
        inner.Setup(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>())).Returns(gate.Task);
        (CachingBitbucketClient sut, _) = CreateSut(inner.Object);

        using CancellationTokenSource cts = new();
        Task<IReadOnlyList<BitbucketRepositoryDto>> cancelled = sut.GetRepositoriesAsync(cts.Token);
        Task<IReadOnlyList<BitbucketRepositoryDto>> patient = sut.GetRepositoriesAsync();

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);

        gate.SetResult(Repos);
        Assert.Same(Repos, await patient);
        inner.Verify(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
