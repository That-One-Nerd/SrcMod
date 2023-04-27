namespace SrcMod.Shell.Valve;

public class VdfSerializer
{
    public VdfOptions Options => p_options;

    private VdfOptions p_options;

    public VdfSerializer() : this(VdfOptions.Default) { }
    public VdfSerializer(VdfOptions options)
    {
        p_options = options;
    }

    public void Serialize(Stream stream, VdfNode parentNode, string parentNodeName)
    {
        StreamWriter writer = new(stream, leaveOpen: !p_options.closeWhenFinished);
        VdfConvert.SerializeNode(writer, parentNode, parentNodeName, p_options, 0);
        writer.Close();

        if (!p_options.closeWhenFinished && p_options.resetStreamPosition) stream.Seek(0, SeekOrigin.Begin);
    }
}
