namespace OneBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ServiceGenericInfoAttribute(TypesGeneric types) : Attribute
    {
        public TypesGeneric Types = types;
    }
    [Flags]
    public enum TypesGeneric
    {
        User = 1,
        DB = 2
    }
}
