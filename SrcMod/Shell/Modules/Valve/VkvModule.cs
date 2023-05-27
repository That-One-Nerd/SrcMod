namespace SrcMod.Shell.Modules.Valve;

[Module("vkv")]
public static class VkvModule
{
    [Command("create")]
    public static void CreateVkv(string path)
    {
        if (File.Exists(path)) throw new($"File already exists at \"{path}\". Did you mean to run \"vkv edit\"?");

        VkvNode parentNode = new VkvTreeNode()
        {
            { "key", new VkvSingleNode("value") }
        };
        string parentNodeName = "tree";

        VkvModifyWhole(ref parentNode, ref parentNodeName);
    }

    private static void VkvModifyWhole(ref VkvNode node, ref string nodeName)
    {
        VkvDisplayNode(node, nodeName);
    }
    private static void VkvDisplayNode(in VkvNode? node, in string nodeName, in int indent = 0)
    {
        int spaceCount = indent * 4;

        if (node is null) return;
        else if (node is VkvSingleNode single)
        {
            Write(new string(' ', spaceCount) + $"\"{nodeName}\"  \"{single.value}\"");
        }
        else if (node is VkvTreeNode tree)
        {
            Write(new string(' ', spaceCount) + $"\"{nodeName}\"\n" + new string(' ', spaceCount) + "{");
            foreach (KeyValuePair<string, VkvNode?> subNode in tree)
            {
                VkvDisplayNode(subNode.Value, subNode.Key, indent + 1);
            }
            Write(new string(' ', spaceCount) + "}");
        }
        else return;
    }
}
