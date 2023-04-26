namespace SrcMod.Shell.Valve;

public class KeyValueNode
{
    public int SubNodeCount => p_subNodes.Count;

    public object value;

    private Dictionary<string, KeyValueNode> p_subNodes;

    internal KeyValueNode()
    {
        value = new();
        p_subNodes = new();
    }

    public KeyValueNode this[string name] => p_subNodes[name];
}
