using Microsoft.Extensions.DependencyInjection;
using Template.Services.Implementations;
using Template.Services.Interfaces;
using Template.Services.Mappers;

namespace Template.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<ItemMapper>();
            services.AddTransient<IItemService, ItemService>();
            services.AddTransient<IItemCatalogService, ItemCatalogService>();
            return services;
        }
    }
}
