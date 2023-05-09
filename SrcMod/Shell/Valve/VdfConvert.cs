namespace SrcMod.Shell.Valve;

public static class VdfConvert
{
    private static readonly Dictionary<string, string> p_escapeCodes = new()
    {
        { "\\", @"\\" }, // This must be first.
        { "\'", @"\'" },
        { "\"", @"\""" },
        { "\0", @"\0" },
        { "\a", @"\a" },
        { "\b", @"\b" },
        { "\f", @"\f" },
        { "\n", @"\n" },
        { "\r", @"\r" },
        { "\t", @"\t" },
        { "\v", @"\v" }
    };

    #region DeserializeNode
    public static VdfNode? DeserializeNode(StreamReader reader) =>
        DeserializeNode(reader, VdfOptions.Default, out _, null);
    public static VdfNode? DeserializeNode(StreamReader reader, VdfOptions options) =>
        DeserializeNode(reader, options, out _, null);

    private static VdfNode? DeserializeNode(StreamReader reader, VdfOptions options, out string name,
        string? first)
    {
        string? header = first ?? (reader.ReadLine()?.Trim());
        if (header is null || string.IsNullOrEmpty(header))
        {
            name = string.Empty;
            return null;
        }

        string[] parts = SplitContent(header, options);
        if (parts.Length > 2) throw new VdfSerializationException("Too many values in node.");

        VdfNode node;

        name = DeserializeString(parts[0], options);
        if (parts.Length == 2)
        {
            object value = DeserializeObject(DeserializeString(parts[1], options));
            node = new VdfSingleNode(value);
        }
        else
        {
            string? next = reader.ReadLine()?.Trim();
            if (next is null) throw new VdfSerializationException("Expected starting '{', found end-of-file.");
            else if (next != "{") throw new VdfSerializationException($"Expected starting '{{', found \"{next}\".");
            VdfTreeNode tree = new();
            string? current;
            while ((current = reader.ReadLine()?.Trim()) is not null)
            {
                if (current == "}") break;
                VdfNode? output = DeserializeNode(reader, options, out string subName, current);
                if (output is null) throw new VdfSerializationException("Error deserializing sub-node.");
                tree[subName] = output;
            }
            if (current is null) throw new VdfSerializationException("Reached end-of-file while deserializing group.");
            node = tree;
        }

        return node;
    }

    private static object DeserializeObject(string content) =>
        TypeParsers.ParseAll(content);
    private static string DeserializeString(string content, VdfOptions options)
    {
        if (options.useQuotes)
        {
            if (!content.StartsWith('\"') || !content.EndsWith('\"'))
                throw new VdfSerializationException("No quotes found around content.");
            content = content[1..^1];
        }
        if (options.useEscapeCodes)
        {
            foreach (KeyValuePair<string, string> escapeCode in p_escapeCodes.Reverse())
                content = content.Replace(escapeCode.Value, escapeCode.Key);
        }
        return content;
    }

    private static string[] SplitContent(string content, VdfOptions options)
    {
        content = content.Replace('\t', ' ');
        if (options.useQuotes)
        {
            List<string> values = new();
            string current = string.Empty;
            bool inQuote = false;
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (c == '\"' && !(i > 0 && content[i - 1] == '\\')) inQuote = !inQuote;

                if (c == ' ' && !inQuote)
                {
                    if (!string.IsNullOrEmpty(current)) values.Add(current);
                    current = string.Empty;
                }
                else current += c;
            }
            if (inQuote) throw new VdfSerializationException("Reached end-of-line while inside quotations.");
            if (!string.IsNullOrEmpty(current)) values.Add(current);
            return values.ToArray();
        }
        else return content.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
    #endregion

    #region FromNodeTree
    public static object? FromNodeTree(Type outputType, VdfNode? node, VdfOptions options)
    {
        if (node is null) return null;

        object? instance = Activator.CreateInstance(outputType);
        if (instance is null) return null;

        IEnumerable<FieldInfo> validFields = from field in outputType.GetFields()
                                             let isPublic = field.IsPublic
                                             let isStatic = field.IsStatic
                                             let isIgnored = field.CustomAttributes.Any(x =>
                                                 x.AttributeType == typeof(VdfIgnoreAttribute))
                                             let isConst = field.IsLiteral
                                             where isPublic && !isStatic && !isIgnored && !isConst
                                             select field;

        IEnumerable<PropertyInfo> validProperties;
        if (options.serializeProperties)
        {
            validProperties = from prop in outputType.GetProperties()
                              let canSet = prop.SetMethod is not null
                              let isPublic = canSet && prop.SetMethod!.IsPublic
                              let isStatic = canSet && prop.SetMethod!.IsStatic
                              let isIgnored = prop.CustomAttributes.Any(x =>
                                  x.AttributeType == typeof(VdfIgnoreAttribute))
                              where canSet && isPublic && !isStatic && !isIgnored
                              select prop;
        }
        else validProperties = Array.Empty<PropertyInfo>();

        foreach (FieldInfo field in validFields)
        {
            // TODO: check if the node tree has that field.

            Type castType = field.FieldType;
            if (TypeParsers.CanParse(instance))
            {
                
            }
        }
        foreach (PropertyInfo prop in validProperties)
        {
            // TODO: check if the node tree has that field.

            Type castType = prop.PropertyType;
            if (TypeParsers.CanParse(instance))
            {

            }
        }

        return null;
    }
    #endregion

    #region SerializeNode
    public static void SerializeNode(StreamWriter writer, VdfNode? node, string name,
        VdfOptions options) => SerializeNode(writer, node, name, options, 0);
    public static void SerializeNode(StreamWriter writer, VdfNode? node, string name) =>
        SerializeNode(writer, node, name, VdfOptions.Default, 0);

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
        string? serializedValue = SerializeObject(node.value);
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
        if (node.SubNodeCount <= 0) return;

        writer.Write(new string(' ', indentLevel));
        writer.WriteLine(SerializeString(name, options));
        writer.WriteLine(new string(' ', indentLevel) + '{');

        foreach (KeyValuePair<string, VdfNode?> subNode in node)
            SerializeNode(writer, subNode.Value, subNode.Key, options, indentLevel + options.indentSize);

        writer.WriteLine(new string(' ', indentLevel) + '}');
    }

    private static string? SerializeObject(object? obj)
    {
        if (obj is null) return null;
        return obj.ToString() ?? string.Empty;
    }

    private static string SerializeString(string content, VdfOptions options)
    {
        if (options.useEscapeCodes)
        {
            foreach (KeyValuePair<string, string> escapeCode in p_escapeCodes)
                content = content.Replace(escapeCode.Key, escapeCode.Value);
        }
        if (options.useQuotes) content = $"\"{content}\"";
        return content;
    }

    #endregion

    #region ToNodeTree
    public static VdfNode? ToNodeTree(object? obj) => ToNodeTree(obj, VdfOptions.Default);
    public static VdfNode? ToNodeTree(object? obj, VdfOptions options)
    {
        if (obj is null) return null;
        Type type = obj.GetType();

        if (type.IsPrimitive || TypeParsers.CanParse(obj)) return new VdfSingleNode(obj);
        else if (type.IsPointer) throw new("Cannot serialize a pointer.");

        VdfTreeNode tree = new();

        if (obj is IVdfConvertible vdf) return vdf.ToNodeTree();
        else if (obj is IDictionary dictionary)
        {
            object[] keys = new object[dictionary.Count],
                     values = new object[dictionary.Count];
            dictionary.Keys.CopyTo(keys, 0);
            dictionary.Values.CopyTo(values, 0);
            for (int i = 0; i < dictionary.Count; i++)
            {
                tree[SerializeObject(keys.GetValue(i))!] = ToNodeTree(values.GetValue(i), options);
            }
            return tree;
        }
        else if (obj is ICollection enumerable)
        {
            int index = 0;
            foreach (object item in enumerable)
            {
                tree[SerializeObject(index)!] = ToNodeTree(item, options);
                index++;
            }
            return tree;
        }

        IEnumerable<FieldInfo> validFields = from field in type.GetFields()
                                             let isPublic = field.IsPublic
                                             let isStatic = field.IsStatic
                                             let isIgnored = field.CustomAttributes.Any(x =>
                                                 x.AttributeType == typeof(VdfIgnoreAttribute))
                                             let isConst = field.IsLiteral
                                             where isPublic && !isStatic && !isIgnored && !isConst
                                             select field;

        IEnumerable<PropertyInfo> validProperties;
        if (options.serializeProperties)
        {
            validProperties = from prop in type.GetProperties()
                              let canGet = prop.GetMethod is not null
                              let isPublic = canGet && prop.GetMethod!.IsPublic
                              let isStatic = canGet && prop.GetMethod!.IsStatic
                              let isIgnored = prop.CustomAttributes.Any(x =>
                                  x.AttributeType == typeof(VdfIgnoreAttribute))
                              where canGet && isPublic && !isStatic && !isIgnored
                              select prop;
        }
        else validProperties = Array.Empty<PropertyInfo>();

        foreach (FieldInfo field in validFields)
        {
            tree[field.Name] = ToNodeTree(field.GetValue(obj), options);
        }
        foreach (PropertyInfo prop in validProperties)
        {
            tree[prop.Name] = ToNodeTree(prop.GetValue(obj), options);
        }

        return tree;
    }
    #endregion
}
