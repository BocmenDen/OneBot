using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneBot.Base;
using OneBot.Extensions;
using OneBot.Models;
using System.Reflection;

namespace OneBot
{
    public class ContextBot<TUser, TDB>() where TUser: BaseUser where TDB : UsersDB<TUser>
    {
        public IHost Host { get; private set; } = null!;
        public Action<DbContextOptionsBuilder> ContextBuilder = null!;

        public object GetService(Type type) => IsInitInvoke(() => Host.Services.GetService(type))!;
        public T GetService<T>() where T : class => IsInitInvoke(() => Host.Services.GetService<T>())!;

        public void Init(Action<DbContextOptionsBuilder> contextBuilder, Action<IConfigurationBuilder>? builderConfig = null, Action<IServiceCollection>? bindsServices = null, IEnumerable<Assembly>? servicesDetect = null)
        {
            servicesDetect ??= [];
            ContextBuilder = contextBuilder;
            Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration(x => builderConfig?.Invoke(x))
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(provider => this);
                foreach (var assemblery in servicesDetect.Append(Assembly.GetAssembly(typeof(ContextBot<TUser, TDB>))!))
                {
                    foreach (var type in assemblery.GetTypes())
                    {
                        services.ApplayService<TUser, TDB>(ContextBuilder, type);
                        // TODO Costum Applay
                    }
                }
                bindsServices?.Invoke(services);
            }).Build();            
        }

        private T IsInitInvoke<T>(Func<T> func)
        {
            CheckInit();
            return func();
        }

        private void CheckInit()
        {
            if (Host == null) throw new Exception("Инцилизация не была произведена");
        }
    }
}
