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

    [Command("edit")]
    public static void EditVkv(string path)
    {
        if (!File.Exists(path)) throw new($"No file exists at \"{path}\". Did you mean to run \"vkv create\"?");

        VkvNode? parentNode;
        string parentNodeName = "this doesn't work yet.";
        try
        {
            FileStream fs = new(path, FileMode.Open);
            parentNode = SerializeVkv.Deserialize(fs);

            if (parentNode is null) throw new("Deserialized VKV node is null.");
        }
        catch
        {
#if DEBUG
            throw;
#else
            throw new($"Error parsing file to Valve KeyValues format: {e.Message}");
#endif
        }

        VkvModifyWhole(ref parentNode, ref parentNodeName);
    }

    private static void VkvModifyWhole(ref VkvNode node, ref string nodeName)
    {
        // TODO
    }
}
