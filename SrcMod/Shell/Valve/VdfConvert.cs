using System.Security.Cryptography;

namespace SrcMod.Shell.Valve;

public static class VdfConvert
{
    private static readonly Dictionary<string, string> p_escapeCodes = new()
    {
        { "\\", "\\\\" },
        { "\'", "\\\'" },
        { "\"", "\\\"" },
        { "\0", "\\0" },
        { "\a", "\\a" },
        { "\b", "\\b" },
        { "\f", "\\f" },
        { "\n", "\\n" },
        { "\r", "\\r" },
        { "\t", "\\t" },
        { "\v", "\\v" }
    };

    public static void SerializeNode(StreamWriter writer, VdfNode? node, string name,
        VdfOptions options) => SerializeNode(writer, node, name, options, 0);
    public static void SerializeNode(StreamWriter writer, VdfNode? node, string name) =>
        SerializeNode(writer, node, name, VdfOptions.Default, 0);

    public static VdfNode? ToNodeTree(object? obj) => ToNodeTree(obj, VdfOptions.Default);
    public static VdfNode? ToNodeTree(object? obj, VdfOptions options)
    {
        return null;
    }

    private static void SerializeNode(StreamWriter writer, VdfNode? node, string name,
        VdfOptions options, int indentLevel)
    {
        if (node is null) return;
        else if (node is VdfSingleNode single) SerializeSingleNode(writer, single, name, options, indentLevel);
        else if (node is VdfTreeNode tree) SerializeTreeNode(writer, tree, name, options, indentLevel);
        else throw new("Unknown node type.");
    }

    private static void SerializeSingleNode(StreamWriter writer, VdfSingleNode node, string name,
        VdfOptions options, int indentLevel)
    {
        string? serializedValue = SerializeObject(node.value, options);
        if (serializedValue is null) return;

        writer.Write(new string(' ', indentLevel));
        writer.Write(SerializeString(name, options));
        writer.Write(' ');

        serializedValue = SerializeString(serializedValue, options);
        writer.WriteLine(serializedValue);
    }
    private static void SerializeTreeNode(StreamWriter writer, VdfTreeNode node, string name,
        VdfOptions options, int indentLevel)
    {
        writer.Write(new string(' ', indentLevel));
        writer.WriteLine(SerializeString(name, options));
        writer.WriteLine(new string(' ', indentLevel) + '{');

        foreach (KeyValuePair<string, VdfNode?> subNode in node)
            SerializeNode(writer, subNode.Value, subNode.Key, options, indentLevel + options.indentSize);

        writer.WriteLine(new string(' ', indentLevel) + '}');
    }

    private static string? SerializeObject(object? obj, VdfOptions options)
    {
        if (obj is null) return null;
        return obj.ToString() ?? string.Empty;
    }

    private static string SerializeString(string content, VdfOptions options)
    {
        if (options.useEscapeCodes)
            foreach (KeyValuePair<string, string> escapeCode in p_escapeCodes)
                content = content.Replace(escapeCode.Key, escapeCode.Value);
        if (options.useQuotes) content = $"\"{content}\"";
        return content;
    }
}
