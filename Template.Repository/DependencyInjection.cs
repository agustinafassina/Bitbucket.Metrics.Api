using Microsoft.Extensions.DependencyInjection;
using Template.Repository.Implementations;
using Template.Repository.Interfaces;

namespace Template.Repository
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddSingleton<IItemRepository, ItemRepository>();
            return services;
        }
    }
}
