using Loxodon.Framework.Services;

public class ServiceConfig : ServiceContainer
{
    /// <summary>
    /// Auto create object T when it doesn't register in container
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public new T Resolve<T>() where T : new()
    {
        var obj = base.Resolve<T>();
        if (obj == null)
        {
            obj = new T();
            base.Register(obj);
        }

        return obj;
    }
}
