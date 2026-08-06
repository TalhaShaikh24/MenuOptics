namespace MenuOptics.API.Middleware
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class IgnoreSubscriptionAttribute : Attribute
    {
    }
}
