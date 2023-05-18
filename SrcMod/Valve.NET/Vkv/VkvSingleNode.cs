namespace Valve.Vkv;

public class VkvSingleNode : VkvNode
{
    public object? value;

    public VkvSingleNode(object? value = null) : base()
    {
        this.value = value;
    }
}
