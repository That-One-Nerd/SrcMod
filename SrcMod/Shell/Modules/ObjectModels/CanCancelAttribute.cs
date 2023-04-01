namespace SrcMod.Shell.Modules.ObjectModels;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class CanCancelAttribute : Attribute
{
    public readonly bool CanCancel;

    public CanCancelAttribute(bool canCancel)
    {
        CanCancel = canCancel;
    }
}
