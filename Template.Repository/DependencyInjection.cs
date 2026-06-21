using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddHttpClient<IBitbucketClient, BitbucketClient>();

            return services;
        }
    }
}
