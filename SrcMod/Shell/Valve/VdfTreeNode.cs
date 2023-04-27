namespace SrcMod.Shell.Valve;

public class VdfTreeNode : VdfNode, IEnumerable<KeyValuePair<string, VdfNode>>
{
    public int SubNodeCount => p_subNodes.Count;

    private Dictionary<string, VdfNode> p_subNodes;

    public VdfTreeNode(Dictionary<string, VdfNode>? subNodes = null) : base()
    {
        p_subNodes = subNodes ?? new();
    }

    public VdfNode this[string key]
    {
        get => p_subNodes[key];
        set
        {
            if (p_subNodes.ContainsKey(key)) p_subNodes[key] = value;
            else p_subNodes.Add(key, value);
        }
    }
    public VdfNode this[int index]
    {
        get => p_subNodes.Values.ElementAt(index);
        set
        {
            if (p_subNodes.Count >= index || index < 0) throw new IndexOutOfRangeException();
            p_subNodes[p_subNodes.Keys.ElementAt(index)] = value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<KeyValuePair<string, VdfNode>> GetEnumerator() => p_subNodes.GetEnumerator();
}
