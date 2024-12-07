using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneBot.Attributes;
using System.Reflection;

namespace OneBot
{
    public static class BotBuilder
    {
        private readonly static object PropertyConnectToDB = "OneBot.BotBuilder_ConnectToDB";

        private static readonly MethodInfo AddDbContextPoolMethod = typeof(EntityFrameworkServiceCollectionExtensions).GetMethod(
                nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextPool),
                1,
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>), typeof(int)],
                null)!;

        public static IHostBuilder CreateDefaultBuilder() => Host.CreateDefaultBuilder();

        public static IHostBuilder RegisterDBContextOptions(this IHostBuilder builder, Action<IConfiguration, DbContextOptionsBuilder> optionsBuilder)
        {
            builder.Properties[PropertyConnectToDB] = optionsBuilder;
            return builder;
        }
        public static IHostBuilder RegisterDBContextOptions(this IHostBuilder builder, Action<DbContextOptionsBuilder> optionsBuilder)
            => builder.RegisterDBContextOptions((_, b) => optionsBuilder(b));

        public static IHostBuilder RegisterServices(this IHostBuilder builder, params Assembly?[] assemblies)
        {
            if (assemblies == null) return builder;
            foreach (var assembly in assemblies)
                builder.RegisterServices(assembly);
            return builder;
        }

        public static IHostBuilder RegisterServices(this IHostBuilder builder, Assembly? assembly)
        {
            if (assembly == null) return builder;
            builder.ConfigureServices((context, services) =>
            {
                foreach (var implementationType in assembly.GetTypes())
                {
                    Attribute? attr;
                    Type type = implementationType;

                    attr = implementationType.GetCustomAttribute(typeof(ServiceAttribute<>));
                    if (attr != null)
                    {
                        type = attr.GetType().GenericTypeArguments[0];
                    }
                    else
                    {
                        attr = implementationType.GetCustomAttribute(typeof(ServiceAttribute));
                        if (attr == null) continue;
                    }
                    switch (((ServiceAttribute)attr).Type)
                    {
                        case ServiceType.Singltone:
                            services.AddSingleton(type, implementationType);
                            break;
                        case ServiceType.Scoped:
                            services.AddScoped(type, implementationType);
                            break;
                        case ServiceType.AddTransient:
                            services.AddTransient(type, implementationType);
                            break;
                        case ServiceType.DbContextPool:
                            if (context.Properties.TryGetValue(PropertyConnectToDB, out object? value) && value is Action<IConfiguration, DbContextOptionsBuilder> dbBuilder)
                            {
                                var method = AddDbContextPoolMethod.MakeGenericMethod(type);
                                Action<DbContextOptionsBuilder> dbBuilderApplayConfig = (b) => dbBuilder(context.Configuration, b);
                                method.Invoke(null, [services, dbBuilderApplayConfig, 1024]);
                            }
                            break;
                    }
                }
            });
            return builder;
        }
    }
}
