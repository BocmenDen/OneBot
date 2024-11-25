using Microsoft.EntityFrameworkCore;
using OneBot.Models;

namespace OneBot.Extensions
{
    public static class SharedExtensions
    {
        public static T CreateElementAndReload<T>(this DbContext context, T value, DbSet<T> table) where T : class
        {
            _ = table.Add(value);
            context.SaveChanges();
            return value;
        }
        public static T GetOrInstance<T>(this CollectionBotParameters collection, string key, Func<T> create) where T : class
        {
            if (collection.TryGetParameter(key, out T? value)) return value!;
            value = create();
            collection[key] = value;
            return value;
        }
        public static T? GetOrDefault<T>(this CollectionBotParameters collection, string key) where T : class
        {
            if (collection.TryGetParameter(key, out T? value)) return value!;
            return default;
        }
    }
}
