using System.Security.Cryptography;

namespace SrcMod.Shell.Valve;

public static class VdfConvert
{
    private static readonly Dictionary<string, string> p_escapeCodes = new()
    {
        { "\'", "\\\'" },
        { "\"", "\\\"" },
        { "\\", "\\\\" },
        { "\0", "\\0" },
        { "\a", "\\a" },
        { "\b", "\\b" },
        { "\f", "\\f" },
        { "\n", "\\n" },
        { "\r", "\\r" },
        { "\t", "\\t" },
        { "\v", "\\v" }
    };

    public static void SerializeNode(StreamWriter writer, VdfNode node, string name,
        VdfOptions options, int indentLevel)
    {
        if (node is VdfSingleNode single) SerializeSingleNode(writer, single, name, options, indentLevel);
        else if (node is VdfTreeNode tree) SerializeTreeNode(writer, tree, name, options, indentLevel);
        else throw new("Unknown node type.");
    }

    private static void SerializeSingleNode(StreamWriter writer, VdfSingleNode node, string name,
        VdfOptions options, int indentLevel)
    {
        writer.Write(new string(' ', indentLevel));
        writer.Write(SerializeString(name, options));
        writer.Write(' ');

        string serializedValue = SerializeString(SerializeObject(node.value, options), options);
        writer.WriteLine(serializedValue);
    }
    private static void SerializeTreeNode(StreamWriter writer, VdfTreeNode node, string name,
        VdfOptions options, int indentLevel)
    {
        writer.Write(new string(' ', indentLevel));
        writer.WriteLine(SerializeString(name, options));
        writer.WriteLine(new string(' ', indentLevel) + '{');

        foreach (KeyValuePair<string, VdfNode> subNode in node)
        {
            if (subNode.Value is VdfSingleNode singleSubNode && singleSubNode.value.GetType().IsArray)
            {
                Array array = (Array)singleSubNode.value;
                Dictionary<string, VdfNode> items = new();
                for (int i = 0; i < array.Length; i++)
                {
                    object? item = array.GetValue(i);
                    if (item is VdfNode subNodeItem) items.Add(i.ToString(), subNodeItem);
                    else items.Add(i.ToString(), new VdfSingleNode(item));
                }
            }
            else SerializeNode(writer, subNode.Value, subNode.Key, options, indentLevel + options.indentSize);
        }

        writer.WriteLine(new string(' ', indentLevel) + '}');
    }

    private static string SerializeObject(object obj, VdfOptions options)
    {
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
