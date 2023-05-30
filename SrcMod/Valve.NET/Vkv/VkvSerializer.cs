namespace Valve.Vkv;

public class VkvSerializer
{
    public VkvOptions Options => p_options;

    private readonly VkvOptions p_options;

    public VkvSerializer() : this(VkvOptions.Default) { }
    public VkvSerializer(VkvOptions options)
    {
        p_options = options;
    }

    public VkvNode? Deserialize(Stream stream)
    {
        long pos = stream.Position;
        StreamReader reader = new(stream, leaveOpen: !p_options.closeWhenFinished);
        try
        {
            VkvNode? result = VkvConvert.DeserializeNode(reader, p_options);
            reader.Close();
            if (!p_options.closeWhenFinished && p_options.resetStreamPosition) stream.Seek(pos, SeekOrigin.Begin);
            return result;
        }
        finally
        {
            reader.Close();
            if (!p_options.closeWhenFinished && p_options.resetStreamPosition) stream.Seek(pos, SeekOrigin.Begin);
        }
    }
    public VkvNode? Deserialize(Stream stream, out string name)
    {
        long pos = stream.Position;
        StreamReader reader = new(stream, leaveOpen: !p_options.closeWhenFinished);
        try
        {
            VkvNode? result = VkvConvert.DeserializeNode(reader, p_options, out name);
            reader.Close();
            if (!p_options.closeWhenFinished && p_options.resetStreamPosition) stream.Seek(pos, SeekOrigin.Begin);
            return result;
        }
        finally
        {
            reader.Close();
            if (!p_options.closeWhenFinished && p_options.resetStreamPosition) stream.Seek(pos, SeekOrigin.Begin);
        }
    }
    public T? Deserialize<T>(Stream stream)
    {
        VkvNode? result = Deserialize(stream);
        return VkvConvert.FromNodeTree<T>(result, p_options);
    }
    public object? Deserialize(Type outputType, Stream stream)
    {
        VkvNode? result = Deserialize(stream);
        return VkvConvert.FromNodeTree(outputType, result, p_options);
    }

    public void Serialize(Stream stream, object? value, string parentNodeName)
    {
        VkvNode? nodeTree = VkvConvert.ToNodeTree(value, p_options);
        Serialize(stream, nodeTree, parentNodeName);
    }
    public void Serialize(Stream stream, VkvNode? parentNode, string parentNodeName)
    {
        long pos = stream.Position;
        StreamWriter writer = new(stream, leaveOpen: !p_options.closeWhenFinished);
        VkvConvert.SerializeNode(writer, parentNode, parentNodeName, p_options);
        writer.Close();

        if (!p_options.closeWhenFinished && p_options.resetStreamPosition) stream.Seek(pos, SeekOrigin.Begin);
    }
}
