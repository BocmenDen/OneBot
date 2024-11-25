using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneBot.Attributes;
using OneBot.Base;
using OneBot.Utils;
using System.Reflection;

namespace OneBot.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static readonly MethodInfo AddDbContextPoolMethod = typeof(EntityFrameworkServiceCollectionExtensions).GetMethod(
                nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextPool),
                1,
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>), typeof(int)],
                null)!;

        public static void ApplayService<TUser, TDB>(this IServiceCollection services, Action<DbContextOptionsBuilder> contextBuilder, Type type)
            where TUser : BaseUser
            where TDB : UsersDB<TUser>
        {
            foreach (var attr in type.GetCustomAttributes())
            {
                Type attrType = attr.GetType();
                if (attr is ServiceAttribute service) // TODO Refactor
                {
                    if (attrType.IsGenericType && attrType.GetGenericTypeDefinition() == typeof(ServiceAttribute<>))
                    {
                        Type serviceType = attrType.GetGenericArguments().First();
                        if (service.IsSingltone)
                            services.AddSingleton(serviceType, BaseUserUtil.ApplayGenericContextType<TUser, TDB>(type));
                        else
                            services.AddTransient(serviceType, BaseUserUtil.ApplayGenericContextType<TUser, TDB>(type));
                        return;
                    }

                    if (service.IsSingltone)
                        services.AddSingleton(BaseUserUtil.ApplayGenericContextType<TUser, TDB>(type));
                    else
                        services.AddTransient(BaseUserUtil.ApplayGenericContextType<TUser, TDB>(type));

                    return;
                }
                else if (attr is ServiceDBAttribute)
                {
                    var method = AddDbContextPoolMethod.MakeGenericMethod(BaseUserUtil.ApplayGenericContextType<TUser, TDB>(type));
                    method.Invoke(null, [services, contextBuilder, 1024]);

                    return;
                }
            }
        }
    }
}
