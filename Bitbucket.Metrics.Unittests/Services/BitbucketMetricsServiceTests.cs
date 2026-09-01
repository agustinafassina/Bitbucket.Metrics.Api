using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Bitbucket.Metrics.Models.Configuration;
using Bitbucket.Metrics.Models.Dto.Bitbucket;
using Bitbucket.Metrics.Repository.Interfaces;
using Bitbucket.Metrics.Services.Implementations;

namespace Bitbucket.Metrics.UnitTests.Services;

public class BitbucketMetricsServiceTests
{
    private static readonly DateTimeOffset Base = new(2024, 6, 3, 10, 0, 0, TimeSpan.Zero); // Monday 10:00 UTC

    private static BitbucketMetricsService CreateSut(IBitbucketClient client, int maxDiffCommits = 300)
        => new(
            client,
            Options.Create(new BitbucketOptions { MaxDiffCommits = maxDiffCommits }),
            NullLogger<BitbucketMetricsService>.Instance);

    private static BitbucketCommitDto Commit(
        string author,
        DateTimeOffset date,
        string repo = "repo1",
        string message = "msg",
        string? email = null,
        string? hash = null) => new()
        {
            Hash = hash ?? Guid.NewGuid().ToString("N"),
            Message = message,
            Date = date,
            AuthorName = author,
            AuthorEmail = email,
            RepositorySlug = repo
        };

    [Fact]
    public async Task GetTopCommitters_counts_orders_and_computes_cadence()
    {
        var commits = new List<BitbucketCommitDto>
        {
            Commit("Ana", Base),
            Commit("Ana", Base.AddDays(2)),
            Commit("Ana", Base.AddDays(4)),
            Commit("Beto", Base.AddDays(1))
        };
        var client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(commits);

        BitbucketMetricsService? sut = CreateSut(client.Object);

        IReadOnlyList<CommitterMetricDto>? result = await sut.GetTopCommittersAsync("repo1");

        Assert.Equal(2, result.Count);
        Assert.Equal("Ana", result[0].Author);
        Assert.Equal(3, result[0].CommitCount);
        Assert.Equal(2d, result[0].AverageDaysBetweenCommits); // (4 days span) / (3-1)
        Assert.Equal("Beto", result[1].Author);
        Assert.Equal(0d, result[1].AverageDaysBetweenCommits);
    }

    [Fact]
    public async Task GetTopCommitters_respects_top_limit()
    {
        List<BitbucketCommitDto>? commits = new List<BitbucketCommitDto>
        {
            Commit("Ana", Base), Commit("Ana", Base),
            Commit("Beto", Base),
            Commit("Caro", Base), Commit("Caro", Base), Commit("Caro", Base)
        };
        var client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(commits);

        IReadOnlyList<CommitterMetricDto>? result = await CreateSut(client.Object).GetTopCommittersAsync("repo1", top: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("Caro", result[0].Author);
        Assert.Equal("Ana", result[1].Author);
    }

    [Fact]
    public async Task GetTopCommitters_aggregates_across_repos_when_slug_null()
    {
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<BitbucketRepositoryDto>
              {
                  new() { Slug = "repo1" },
                  new() { Slug = "repo2" }
              });
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<BitbucketCommitDto> { Commit("Ana", Base, "repo1") });
        client.Setup(c => c.GetCommitsAsync("repo2", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<BitbucketCommitDto> { Commit("Ana", Base, "repo2") });

        IReadOnlyList<CommitterMetricDto>? result = await CreateSut(client.Object).GetTopCommittersAsync();

        CommitterMetricDto? ana = Assert.Single(result);
        Assert.Equal(2, ana.CommitCount);
        Assert.Equal(new[] { "repo1", "repo2" }, ana.Repositories);
    }

    [Fact]
    public async Task GetCommitFrequency_buckets_by_day()
    {
        List<BitbucketCommitDto>? commits = new List<BitbucketCommitDto>
        {
            Commit("Ana", Base),
            Commit("Beto", Base.AddHours(3)),
            Commit("Ana", Base.AddDays(1))
        };
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(commits);

        IReadOnlyList<CommitActivityPointDto>? result = await CreateSut(client.Object).GetCommitFrequencyAsync("repo1", interval: "day");

        Assert.Equal(2, result.Count);
        Assert.Equal("2024-06-03", result[0].Period);
        Assert.Equal(2, result[0].CommitCount);
        Assert.Equal(2, result[0].ContributorCount);
        Assert.Equal("2024-06-04", result[1].Period);
        Assert.Equal(1, result[1].CommitCount);
    }

    [Fact]
    public async Task GetCommitFrequency_buckets_by_month()
    {
        List<BitbucketCommitDto>? commits = new List<BitbucketCommitDto>
        {
            Commit("Ana", new DateTimeOffset(2024, 6, 10, 0, 0, 0, TimeSpan.Zero)),
            Commit("Ana", new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero))
        };
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(commits);

        IReadOnlyList<CommitActivityPointDto>? result = await CreateSut(client.Object).GetCommitFrequencyAsync("repo1", interval: "month");

        Assert.Equal(new[] { "2024-06", "2024-07" }, result.Select(r => r.Period));
    }

    [Fact]
    public async Task GetActivityHeatmap_groups_by_day_and_hour()
    {
        List<BitbucketCommitDto>? commits = new List<BitbucketCommitDto>
        {
            Commit("Ana", Base),               // Monday 10
            Commit("Beto", Base.AddMinutes(20)), // Monday 10
            Commit("Ana", Base.AddHours(5))     // Monday 15
        };
        var client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(commits);

        IReadOnlyList<CommitHeatmapPointDto>? result = await CreateSut(client.Object).GetActivityHeatmapAsync("repo1");

        CommitHeatmapPointDto? monday10 = Assert.Single(result, p => p.DayOfWeek == 1 && p.Hour == 10);
        Assert.Equal(2, monday10.CommitCount);
        Assert.Equal("Monday", monday10.DayName);
        Assert.Contains(result, p => p.DayOfWeek == 1 && p.Hour == 15 && p.CommitCount == 1);
    }

    [Fact]
    public async Task GetIssueActivity_extracts_jira_keys_from_messages()
    {
        List<BitbucketCommitDto>? commits = new List<BitbucketCommitDto>
        {
            Commit("Ana", Base, message: "PROJ-1 initial work"),
            Commit("Beto", Base.AddDays(1), message: "fix for PROJ-1 and AB-22"),
            Commit("Ana", Base.AddDays(2), message: "no issue key here")
        };
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(commits);

        IReadOnlyList<IssueActivityDto>? result = await CreateSut(client.Object).GetIssueActivityAsync("repo1");

        Assert.Equal(2, result.Count);
        IssueActivityDto? proj1 = Assert.Single(result, i => i.IssueKey == "PROJ-1");
        Assert.Equal(2, proj1.CommitCount);
        Assert.Equal(new[] { "Ana", "Beto" }, proj1.Authors);
        Assert.Contains(result, i => i.IssueKey == "AB-22" && i.CommitCount == 1);
    }

    [Fact]
    public async Task GetPullRequestMetrics_computes_counts_and_merge_times()
    {
        List<BitbucketPullRequestDto>? prs = new List<BitbucketPullRequestDto>
        {
            new() { Id = 1, State = "MERGED", Author = "Ana", HoursToMerge = 2 },
            new() { Id = 2, State = "MERGED", Author = "Ana", HoursToMerge = 4 },
            new() { Id = 3, State = "MERGED", Author = "Beto", HoursToMerge = 12 },
            new() { Id = 4, State = "OPEN", Author = "Beto" },
            new() { Id = 5, State = "DECLINED", Author = "Caro" }
        };
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetPullRequestsAsync("repo1", It.IsAny<IEnumerable<string>>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(prs);

        PullRequestMetricsDto? result = await CreateSut(client.Object).GetPullRequestMetricsAsync("repo1");

        Assert.Equal(1, result.TotalOpen);
        Assert.Equal(3, result.TotalMerged);
        Assert.Equal(1, result.TotalDeclined);
        Assert.Equal(6d, result.AverageHoursToMerge); // (2+4+12)/3
        Assert.Equal(4d, result.MedianHoursToMerge);   // sorted 2,4,12 -> 4
        AuthorPullRequestStatDto? ana = Assert.Single(result.ByAuthor, a => a.Author == "Ana");
        Assert.Equal(2, ana.Merged);
        Assert.Equal(3d, ana.AverageHoursToMerge);
    }

    [Fact]
    public async Task GetReviewerLeaderboard_aggregates_across_repos()
    {
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<BitbucketRepositoryDto> { new() { Slug = "repo1" }, new() { Slug = "repo2" } });
        client.Setup(c => c.GetReviewerStatsAsync("repo1", It.IsAny<IEnumerable<string>>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ReviewerMetricDto> { new() { Reviewer = "Ana", PullRequestsReviewed = 2, Approvals = 1 } });
        client.Setup(c => c.GetReviewerStatsAsync("repo2", It.IsAny<IEnumerable<string>>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ReviewerMetricDto> { new() { Reviewer = "Ana", PullRequestsReviewed = 3, Approvals = 2 } });

        IReadOnlyList<ReviewerMetricDto>? result = await CreateSut(client.Object).GetReviewerLeaderboardAsync();

        ReviewerMetricDto? ana = Assert.Single(result);
        Assert.Equal(5, ana.PullRequestsReviewed);
        Assert.Equal(3, ana.Approvals);
    }

    [Fact]
    public async Task GetChurn_sums_diffstat_per_author()
    {
        List<BitbucketCommitDto>? commits = new List<BitbucketCommitDto>
        {
            Commit("Ana", Base, hash: "a1"),
            Commit("Ana", Base.AddDays(1), hash: "a2"),
            Commit("Beto", Base.AddDays(2), hash: "b1")
        };
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(commits);
        client.Setup(c => c.GetCommitDiffStatAsync("repo1", "a1", It.IsAny<CancellationToken>())).ReturnsAsync((10, 2));
        client.Setup(c => c.GetCommitDiffStatAsync("repo1", "a2", It.IsAny<CancellationToken>())).ReturnsAsync((5, 1));
        client.Setup(c => c.GetCommitDiffStatAsync("repo1", "b1", It.IsAny<CancellationToken>())).ReturnsAsync((20, 20));

        IReadOnlyList<ChurnMetricDto>? result = await CreateSut(client.Object).GetChurnAsync("repo1");

        ChurnMetricDto? ana = Assert.Single(result, c => c.Author == "Ana");
        Assert.Equal(2, ana.Commits);
        Assert.Equal(15, ana.LinesAdded);
        Assert.Equal(3, ana.LinesRemoved);
        Assert.Equal(12, ana.NetLines);
        ChurnMetricDto? beto = Assert.Single(result, c => c.Author == "Beto");
        Assert.Equal(40, beto.TotalChanges);
    }

    [Fact]
    public async Task GetChurn_caps_at_max_diff_commits()
    {
        List<BitbucketCommitDto>? commits = new List<BitbucketCommitDto>
        {
            Commit("Ana", Base, hash: "a1"),
            Commit("Ana", Base.AddDays(1), hash: "a2"),
            Commit("Ana", Base.AddDays(2), hash: "a3")
        };
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(commits);
        client.Setup(c => c.GetCommitDiffStatAsync("repo1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((1, 0));

        IReadOnlyList<ChurnMetricDto>? result = await CreateSut(client.Object, maxDiffCommits: 2).GetChurnAsync("repo1");

        ChurnMetricDto? ana = Assert.Single(result);
        Assert.Equal(2, ana.Commits); // only 2 most recent processed
        client.Verify(c => c.GetCommitDiffStatAsync("repo1", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetWorkspaceSummary_aggregates_repos_and_commits()
    {
        Mock<IBitbucketClient>? client = new Mock<IBitbucketClient>();
        client.Setup(c => c.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<BitbucketRepositoryDto> { new() { Slug = "repo1" }, new() { Slug = "repo2" } });
        client.Setup(c => c.GetCommitsAsync("repo1", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<BitbucketCommitDto>
              {
                  Commit("Ana", Base, "repo1", "PROJ-1 work"),
                  Commit("Beto", Base.AddMinutes(5), "repo1")
              });
        client.Setup(c => c.GetCommitsAsync("repo2", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<BitbucketCommitDto> { Commit("Ana", Base.AddDays(1), "repo2") });

        WorkspaceSummaryDto? result = await CreateSut(client.Object).GetWorkspaceSummaryAsync();

        Assert.Equal(2, result.RepositoryCount);
        Assert.Equal(3, result.CommitCount);
        Assert.Equal(2, result.ContributorCount);
        Assert.Equal(1, result.LinkedIssueCount);
        Assert.Equal("Monday", result.BusiestDay);
        Assert.Equal(10, result.BusiestHour);
        Assert.Equal("Ana", result.TopCommitters[0].Author);
    }
}
