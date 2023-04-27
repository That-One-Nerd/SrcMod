namespace SrcMod.Shell.Valve;

interface static class VdfConvert
{
    private static readonly Dictionary<string, string> p_escapeCodes = new()
    {
        { "\'", "\\\'" },
        { "\"", "\\\"" },
        { "\\", "\\\\" },
        { "\0", "\\\0" },
        { "\a", "\\\a" },
        { "\b", "\\\b" },
        { "\f", "\\\f" },
        { "\n", "\\\n" },
        { "\r", "\\\r" },
        { "\t", "\\\t" },
        { "\v", "\\\v" }
    };

    public static void SerializeNode(StreamWriter writer, VdfNode node, string name,
        ref VdfOptions options, int indentLevel)
    {
        if (node is VdfSingleNode single) SerializeSingleNode(writer, single, name, ref options, indentLevel);
        else if (node is VdfTreeNode tree) SerializeTreeNode(writer, tree, name, ref options, indentLevel);
        else throw new("Unknown node type.");
    }

    private static void SerializeSingleNode(StreamWriter writer, VdfSingleNode node, string name,
        ref VdfOptions options, int indentLevel)
    {
        string serializedName = SerializeString(name, ref options),
               serializedValue = SerializeObject(node.value, ref options);
        writer.WriteLine($"{new string(' ', indentLevel)}{serializedName} {serializedValue}");
    }
    private static void SerializeTreeNode(StreamWriter writer, VdfTreeNode node, string name,
        ref VdfOptions options, int indentLevel)
    {
        string serializedName = SerializeString(name, ref options),
               serializedValue = ""; // TODO: serialize each value with a higher indent

        string indent = new(' ', indentLevel);
        writer.WriteLine($"{indent}{serializedName}\n{indent}{{\n{serializedValue}\n{indent}}}");
    }

    private static string SerializeObject(object obj, ref VdfOptions options)
    {
        // TODO: serialize an object
        return "";
    }

    private static string SerializeString(string content, ref VdfOptions options)
    {
        if (options.useEscapeCodes)
            foreach (KeyValuePair<string, string> escapeCode in p_escapeCodes)
                content = content.Replace(escapeCode.Key, escapeCode.Value);
        return content;
    }
}
