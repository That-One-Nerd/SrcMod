namespace SrcMod.Shell.Valve;

public class VdfSingleNode : VdfNode
{
    public object? value;

    public VdfSingleNode(object? value = null) : base()
    {
        this.value = value;
    }
}
