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
        StreamWriter writer = p_options.closeWhenFinished ? new(stream) : new(stream, leaveOpen: true);
        VdfConvert.SerializeNode(writer, parentNode, parentNodeName, ref p_options, 0);
        writer.Close();
    }
}
