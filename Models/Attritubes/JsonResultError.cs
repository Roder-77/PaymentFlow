namespace Models.Attritubes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class JsonResultError : Attribute
    {
    }
}
