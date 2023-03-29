namespace SrcMod.Shell.Modules.ObjectModels;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class CommandAttribute : Attribute
{
    public readonly string NameId;

    public CommandAttribute(string nameId)
    {
        NameId = nameId;
    }
}
