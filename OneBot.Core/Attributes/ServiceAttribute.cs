namespace OneBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        public ServiceType Type = ServiceType.Singltone;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute<T>: ServiceAttribute
    {
    }

    public enum ServiceType
    {
        Singltone,
        Scoped,
        AddTransient,
        DbContextPool
    }
}
