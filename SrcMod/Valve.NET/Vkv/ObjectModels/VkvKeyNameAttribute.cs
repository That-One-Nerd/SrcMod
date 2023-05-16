namespace Valve.Vkv.ObjectModels;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class VkvKeyNameAttribute : Attribute
{
    public readonly string name;

    public VkvKeyNameAttribute(string name) => this.name = name;
}
