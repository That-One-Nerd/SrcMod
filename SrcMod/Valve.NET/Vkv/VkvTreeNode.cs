namespace Valve.Vkv;

public class VkvTreeNode : VkvNode, IEnumerable<KeyValuePair<string, VkvNode?>>
{
    public int SubNodeCount => p_subNodes.Count;

    private readonly Dictionary<string, VkvNode?> p_subNodes;

    public VkvTreeNode(Dictionary<string, VkvNode?>? subNodes = null) : base()
    {
        p_subNodes = subNodes ?? new();
    }

    public VkvNode? this[string key]
    {
        get
        {
            if (p_subNodes.TryGetValue(key, out VkvNode? value)) return value;
            else return null;
        }
        set
        {
            if (p_subNodes.ContainsKey(key)) p_subNodes[key] = value;
            else p_subNodes.Add(key, value);
        }
    }
    public VkvNode? this[int index]
    {
        get
        {
            if (p_subNodes.Count >= index || index < 0) return null;
            return p_subNodes.Values.ElementAt(index);
        }
        set
        {
            if (p_subNodes.Count >= index || index < 0) throw new IndexOutOfRangeException();
            p_subNodes[p_subNodes.Keys.ElementAt(index)] = value;
        }
    }

    public void Add(string key, VkvNode? value) => this[key] = value;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<KeyValuePair<string, VkvNode?>> GetEnumerator() => p_subNodes.GetEnumerator();
}
