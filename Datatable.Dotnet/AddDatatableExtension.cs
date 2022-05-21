using Datatable.Dotnet.Fluent;
using Datatable.Dotnet.Setting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Datatable.Dotnet
{
    public static class AddDatatableExtension
    {
        public static IServiceCollection AddDatatable(this IServiceCollection services,IConfigurationSection configurationSection)
        {
            services.AddScoped(typeof(IDatatableBuilder<>), typeof(DatatableBuilder<>));
            services.Configure<DatatableSetting>(configurationSection);
            return services;
        }
    }
}
