using Microsoft.Extensions.DependencyInjection;

namespace CitizenFX.DataLayer
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDbTaskFactory<T>(this IServiceCollection collection)
        {
            return collection.AddSingleton(typeof(T));
        }
    }
}