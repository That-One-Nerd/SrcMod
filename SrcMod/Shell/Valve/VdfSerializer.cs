namespace SrcMod.Shell.Valve;

public class VdfSerializer
{
    public VdfOptions Options => p_options;

    private readonly VdfOptions p_options;

    public VdfSerializer() : this(VdfOptions.Default) { }
    public VdfSerializer(VdfOptions options)
    {
        p_options = options;
    }

    public VdfNode? Deserialize(Stream stream)
    {
        long pos = stream.Position;
        StreamReader reader = new(stream, leaveOpen: !p_options.closeWhenFinished);
        VdfNode? result = VdfConvert.DeserializeNode(reader, p_options);
        reader.Close();

        if (!p_options.closeWhenFinished && p_options.resetStreamPosition) stream.Seek(pos, SeekOrigin.Begin);
        return result;
    }

    public void Serialize(Stream stream, object? value, string parentNodeName)
    {
        VdfNode? nodeTree = VdfConvert.ToNodeTree(value, p_options);
        Serialize(stream, nodeTree, parentNodeName);
    }
    public void Serialize(Stream stream, VdfNode? parentNode, string parentNodeName)
    {
        long pos = stream.Position;
        StreamWriter writer = new(stream, leaveOpen: !p_options.closeWhenFinished);
        VdfConvert.SerializeNode(writer, parentNode, parentNodeName, p_options);
        writer.Close();

        if (!p_options.closeWhenFinished && p_options.resetStreamPosition) stream.Seek(pos, SeekOrigin.Begin);
    }
}
