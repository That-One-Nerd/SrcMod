namespace Valve.Vkv;

public class VkvTreeNode : VkvNode, IEnumerable<KeyValuePair<string, VkvNode>>
{
    public int SubNodeCount => p_subNodes.Count;

    // These should never get out of sync, or bad things will happen.
    private readonly List<string> p_subNodeKeys;
    private readonly List<VkvNode> p_subNodes;

    public VkvTreeNode(Dictionary<string, VkvNode?>? subNodes = null) : base()
    {
        p_subNodeKeys = new();
        p_subNodes = new();

        if (subNodes is not null)
        {
            for (int i = 0; i < subNodes.Count; i++)
            {
                string key = subNodes.Keys.ElementAt(i);
                VkvNode? value = subNodes.Values.ElementAt(i);
                
                if (value is not null)
                {
                    p_subNodeKeys.Add(key);
                    p_subNodes.Add(value);
                }
            }
        }
    }

    public VkvNode? this[string key]
    {
        get
        {
            int index = p_subNodeKeys.IndexOf(key);

            if (index == -1) return null;
            else return p_subNodes[index];
        }
        set
        {
            int index = p_subNodeKeys.IndexOf(key);

            if (index == -1)
            {
                if (value is null) return;

                p_subNodeKeys.Add(key);
                p_subNodes.Add(value);
            }
            else
            {
                if (value is null)
                {
                    p_subNodeKeys.RemoveAt(index);
                    p_subNodes.RemoveAt(index);
                }
                else p_subNodes[index] = value;
            }
        }
    }
    public VkvNode? this[int index]
    {
        get
        {
            if (p_subNodes.Count >= index || index < 0) return null;
            return p_subNodes[index];
        }
        set
        {
            if (p_subNodes.Count >= index || index < 0) throw new IndexOutOfRangeException();

            if (value is null)
            {
                p_subNodeKeys.RemoveAt(index);
                p_subNodes.RemoveAt(index);
            }
            else p_subNodes[index] = value;
        }
    }
    public KeyValuePair<string, VkvNode>? this[Func<int, KeyValuePair<string, VkvNode>, bool> predicate]
    {
        get
        {
            for (int i = 0; i < SubNodeCount; i++)
            {
                KeyValuePair<string, VkvNode> pair = new(p_subNodeKeys[i], p_subNodes[i]);
                if (predicate(i, pair)) return pair;
            }
            return null;
        }
        set
        {
            for (int i = 0; i < SubNodeCount; i++)
            {
                KeyValuePair<string, VkvNode> pair = new(p_subNodeKeys[i], p_subNodes[i]);
                if (predicate(i, pair))
                {
                    if (value.HasValue)
                    {
                        p_subNodeKeys[i] = value.Value.Key;
                        p_subNodes[i] = value.Value.Value;
                    }
                    else
                    {
                        p_subNodeKeys.RemoveAt(i);
                        p_subNodes.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }

    public void Add(string key, VkvNode value) => this[key] = value;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<KeyValuePair<string, VkvNode>> GetEnumerator()
    {
        for (int i = 0; i < SubNodeCount; i++)
        {
            yield return new(p_subNodeKeys[i], p_subNodes[i]);
        }
    }
}
