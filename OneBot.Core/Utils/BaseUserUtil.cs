using OneBot.Base;

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
    }
}
