using Microsoft.Extensions.DependencyInjection;
using Bitbucket.Metrics.Services.Implementations;
using Bitbucket.Metrics.Services.Interfaces;

namespace Bitbucket.Metrics.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddTransient<IBitbucketMetricsService, BitbucketMetricsService>();
            return services;
        }
    }
}
