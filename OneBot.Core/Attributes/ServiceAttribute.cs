namespace OneBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute(bool isSingltone = true) : Attribute
    {
        public readonly bool IsSingltone = isSingltone;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute<T>(bool isSingltone = true) : ServiceAttribute(isSingltone)
    {
    }
}
