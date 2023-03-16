namespace SrcMod.Shell.Modules.ObjectModels;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ModuleAttribute : Attribute
{
    public readonly bool NameIsPrefix;
    public readonly string NameId;

    public ModuleAttribute(string nameId, bool nameIsPrefix = true)
    {
        NameId = nameId;
        NameIsPrefix = nameIsPrefix;
    }
}
