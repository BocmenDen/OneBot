using OneBot.Attributes;
using OneBot.Base;
using System.Reflection;

namespace OneBot.Utils
{
    public static class BaseUserUtil
    {
        private static readonly Dictionary<Type, BaseUser> _factory = [];

        public static TUser CreateEmptyUser<TUser>() where TUser : BaseUser
        {
            if (_factory.TryGetValue(typeof(TUser), out var fabric)) return (fabric.CreateEmpty() as TUser)!;
            _factory[typeof(TUser)] = Activator.CreateInstance<TUser>();
            return CreateEmptyUser<TUser>();
        }

        public static Type ApplayGenericContextType<T, TUser, TDB>() where TUser : BaseUser => ApplayGenericContextType<TUser, TDB>(typeof(T));
        public static Type ApplayGenericContextType<TUser, TDB>(Type type) where TUser : BaseUser
        {
            ServiceGenericInfoAttribute? genericAttr = type.GetCustomAttribute<ServiceGenericInfoAttribute>();
            if (genericAttr == null) return type;
            if (genericAttr.Types == (TypesGeneric.User | TypesGeneric.DB))
                return type.MakeGenericType(typeof(TUser), typeof(TDB));
            else if (genericAttr.Types == TypesGeneric.User)
                return type.MakeGenericType(typeof(TUser));
            else
                return type.MakeGenericType(typeof(TDB));
        }
    }
}
