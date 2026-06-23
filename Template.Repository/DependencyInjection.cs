using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Template.Models.Configuration;
using Template.Repository.Implementations;
using Template.Repository.Interfaces;

namespace Template.Repository
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IItemRepository, ItemRepository>();

            services.Configure<BitbucketOptions>(configuration.GetSection(BitbucketOptions.SectionName));

            int retryCount = configuration.GetSection(BitbucketOptions.SectionName).GetValue<int?>(nameof(BitbucketOptions.RetryCount)) ?? 3;

            services.AddMemoryCache();

            services.AddHttpClient<BitbucketClient>()
                    .AddPolicyHandler(GetRetryPolicy(retryCount));

            services.AddTransient<IBitbucketClient>(sp => new CachingBitbucketClient(
                sp.GetRequiredService<BitbucketClient>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BitbucketOptions>>()));

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => (int)response.StatusCode == 429)
                .WaitAndRetryAsync(
                    retryCount <= 0 ? 1 : retryCount,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
}
