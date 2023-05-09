namespace Valve.Vkv.ObjectModels;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class VkvIgnoreAttribute : Attribute { }
