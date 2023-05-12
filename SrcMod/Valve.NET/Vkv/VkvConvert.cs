using Valve.Vkv.ObjectModels;

namespace Valve.Vkv;

public static class VkvConvert
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
    public static VkvNode? DeserializeNode(StreamReader reader) =>
        DeserializeNode(reader, VkvOptions.Default, out _, null);
    public static VkvNode? DeserializeNode(StreamReader reader, VkvOptions options) =>
        DeserializeNode(reader, options, out _, null);

    private static VkvNode? DeserializeNode(StreamReader reader, VkvOptions options, out string name,
        string? first)
    {
        string? header = first ?? (reader.ReadLine()?.Trim());
        if (header is null || string.IsNullOrEmpty(header))
        {
            name = string.Empty;
            return null;
        }

        string[] parts = SplitContent(header, options);
        if (parts.Length > 2) throw new VkvSerializationException("Too many values in node.");

        VkvNode node;

        name = DeserializeString(parts[0], options);
        if (parts.Length == 2)
        {
            string value = DeserializeString(parts[1], options);
            node = new VkvSingleNode(value);
        }
        else
        {
            string? next = reader.ReadLine()?.Trim();
            if (next is null) throw new VkvSerializationException("Expected starting '{', found end-of-file.");
            else if (next != "{") throw new VkvSerializationException($"Expected starting '{{', found \"{next}\".");
            VkvTreeNode tree = new();
            string? current;
            while ((current = reader.ReadLine()?.Trim()) is not null)
            {
                if (current == "}") break;
                VkvNode? output = DeserializeNode(reader, options, out string subName, current);
                tree[subName] = output;
            }
            if (current is null) throw new VkvSerializationException("Reached end-of-file while deserializing group.");
            node = tree;
        }

        return node;
    }

    private static string DeserializeString(string content, VkvOptions options)
    {
        if (options.useQuotes)
        {
            if (!content.StartsWith('\"') || !content.EndsWith('\"'))
                throw new VkvSerializationException("No quotes found around content.");
            content = content[1..^1];
        }
        if (options.useEscapeCodes)
        {
            foreach (KeyValuePair<string, string> escapeCode in p_escapeCodes.Reverse())
                content = content.Replace(escapeCode.Value, escapeCode.Key);
        }
        return content;
    }

    private static string[] SplitContent(string content, VkvOptions options)
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
            if (inQuote) throw new VkvSerializationException("Reached end-of-line while inside quotations.");
            if (!string.IsNullOrEmpty(current)) values.Add(current);
            return values.ToArray();
        }
        else return content.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
    #endregion

    #region FromNodeTree
    public static T? FromNodeTree<T>(VkvNode? node, VkvOptions options) => (T?)FromNodeTree(typeof(T), node, options);
    public static object? FromNodeTree(Type outputType, VkvNode? node, VkvOptions options)
    {
        if (node is null) return null;

        if (node is VkvSingleNode single) return FromSingleNode(outputType, single);
        else if (node is VkvTreeNode tree) return FromTreeNode(outputType, tree, options);
        else throw new VkvSerializationException("Unknown VKV node type.");
    }

    private static object? FromSingleNode(Type outputType, VkvSingleNode node)
    {
        object? value = node.value;
        if (value is null) return null;
        else if (value is string str)
        {
            value = TypeParsers.ParseAll(str);
            if (value is string still && outputType.IsEnum)
            {
                if (Enum.TryParse(outputType, still, true, out object? res) && res is not null)
                    value = res;
            }
        }
        return Convert.ChangeType(value, outputType);
    }
    
    private static object? FromTreeNode(Type outputType, VkvTreeNode node, VkvOptions options)
    {
        if (outputType.IsArray)
            return FromTreeNodeArray(outputType, node, options);

        else if (outputType.GetInterface("IList") is not null)
            return FromTreeNodeList(outputType, node, options);

        else if (outputType.GetInterface("IDictionary") is not null)
            return FromTreeNodeDictionary(outputType, node, options);

        object? instance = Activator.CreateInstance(outputType);
        if (instance is null) return null;

        IEnumerable<FieldInfo> validFields = from field in outputType.GetFields()
                                             let isPublic = field.IsPublic
                                             let isStatic = field.IsStatic
                                             let isIgnored = field.CustomAttributes.Any(x =>
                                                 x.AttributeType == typeof(VkvIgnoreAttribute))
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
                                  x.AttributeType == typeof(VkvIgnoreAttribute))
                              where canSet && isPublic && !isStatic && !isIgnored
                              select prop;
        }
        else validProperties = Array.Empty<PropertyInfo>();

        foreach (FieldInfo field in validFields)
        {
            string name = field.Name;

            VkvNode? subNode = node[name];
            if (subNode is null) continue;

            object? result = FromNodeTree(field.FieldType, subNode, options);
            if (result is null) continue;
            field.SetValue(instance, result);
        }
        foreach (PropertyInfo prop in validProperties)
        {
            string name = prop.Name;

            VkvNode? subNode = node[name];
            if (subNode is null) continue;

            object? result = FromNodeTree(prop.PropertyType, subNode, options);
            if (result is null) continue;
            prop.SetValue(instance, result);
        }

        return instance;
    }

    private static object? FromTreeNodeArray(Type outputType, VkvTreeNode node, VkvOptions options)
    {
        Type elementType = outputType.GetElementType()!;
        Array array = Array.CreateInstance(elementType, node.SubNodeCount);

        int index = 0;
        foreach (KeyValuePair<string, VkvNode?> subNode in node)
        {
            string indexStr = index.ToString();
            if (subNode.Key != indexStr) throw new VkvSerializationException($"Cannot convert node tree to array.");
            array.SetValue(FromNodeTree(elementType, subNode.Value, options), index);
            index++;
        }
        return array;
    }

    private static object? FromTreeNodeList(Type outputType, VkvTreeNode node, VkvOptions options)
    {
        IList? instance = (IList?)Activator.CreateInstance(outputType);
        if (instance is null) return null;

        // There is no guarentee that the first type argument corresponds to the element type,
        // but as far as I know there isn't a better way.
        Type elementType = outputType.IsGenericType ? outputType.GenericTypeArguments[0] : typeof(object);

        int index = 0;
        foreach (KeyValuePair<string, VkvNode?> subNode in node)
        {
            string indexStr = index.ToString();
            if (subNode.Key != indexStr) throw new VkvSerializationException($"Cannot convert node tree to array.");
            instance.Add(FromNodeTree(elementType, subNode.Value, options));
            index++;
        }
        return instance;
    }

    private static object? FromTreeNodeDictionary(Type outputType, VkvTreeNode node, VkvOptions options)
    {
        IDictionary? instance = (IDictionary?)Activator.CreateInstance(outputType);
        if (instance is null) return null;

        // There is no guarentee that the first and second type arguments represent the
        // key and value types, but as far as I know there isn't a better way.
        bool canUseGenerics = outputType.GenericTypeArguments.Length >= 2;
        Type keyType = canUseGenerics ? outputType.GenericTypeArguments[0] : typeof(object),
             valueType = canUseGenerics ? outputType.GenericTypeArguments[1] : typeof(object);

        foreach (KeyValuePair<string, VkvNode?> subNode in node)
        {
            object key = TypeParsers.ParseAll(subNode.Key);
            if (key is string still && keyType.IsEnum)
            {
                if (Enum.TryParse(keyType, still, true, out object? res) && res is not null)
                    key = res;
            }
            key = Convert.ChangeType(key, keyType);

            object? value = FromNodeTree(valueType, subNode.Value, options);

            instance.Add(key, value);
        }
        return instance;
    }
    #endregion

    #region SerializeNode
    public static void SerializeNode(StreamWriter writer, VkvNode? node, string name,
        VkvOptions options) => SerializeNode(writer, node, name, options, 0);
    public static void SerializeNode(StreamWriter writer, VkvNode? node, string name) =>
        SerializeNode(writer, node, name, VkvOptions.Default, 0);

    private static void SerializeNode(StreamWriter writer, VkvNode? node, string name,
        VkvOptions options, int indentLevel)
    {
        if (node is null) return;
        else if (node is VkvSingleNode single) SerializeSingleNode(writer, single, name, options, indentLevel);
        else if (node is VkvTreeNode tree) SerializeTreeNode(writer, tree, name, options, indentLevel);
        else throw new("Unknown node type.");
    }

    private static void SerializeSingleNode(StreamWriter writer, VkvSingleNode node, string name,
        VkvOptions options, int indentLevel)
    {
        string? serializedValue = SerializeObject(node.value);
        if (serializedValue is null) return;

        writer.Write(new string(' ', indentLevel));
        writer.Write(SerializeString(name, options));

        switch (options.spacing)
        {
            case SpacingMode.SingleSpace: writer.Write(' ');
                break;

            case SpacingMode.IndentSizeSpacing: writer.Write(new string(' ', options.indentSize));
                break;

            case SpacingMode.DoubleTab: writer.Write("\t\t");
                break;

            default: throw new VkvSerializationException($"Unknown spacing mode \"{options.spacing}\".");
        }

        serializedValue = SerializeString(serializedValue, options);
        writer.WriteLine(serializedValue);
    }
    private static void SerializeTreeNode(StreamWriter writer, VkvTreeNode node, string name,
        VkvOptions options, int indentLevel)
    {
        if (node.SubNodeCount <= 0) return;

        writer.Write(new string(' ', indentLevel));
        writer.WriteLine(SerializeString(name, options));
        writer.WriteLine(new string(' ', indentLevel) + '{');

        foreach (KeyValuePair<string, VkvNode?> subNode in node)
            SerializeNode(writer, subNode.Value, subNode.Key, options, indentLevel + options.indentSize);

        writer.WriteLine(new string(' ', indentLevel) + '}');
    }

    private static string? SerializeObject(object? obj)
    {
        if (obj is null) return null;
        return obj.ToString() ?? string.Empty;
    }

    private static string SerializeString(string content, VkvOptions options)
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
    public static VkvNode? ToNodeTree(object? obj) => ToNodeTree(obj, VkvOptions.Default);
    public static VkvNode? ToNodeTree(object? obj, VkvOptions options)
    {
        if (obj is null) return null;
        Type type = obj.GetType();

        if (type.IsPrimitive || TypeParsers.CanParse(obj)) return new VkvSingleNode(obj);
        else if (type.IsPointer) throw new("Cannot serialize a pointer.");

        VkvTreeNode tree = new();

        if (obj is IVkvConvertible vkv) return vkv.ToNodeTree();
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
                                                 x.AttributeType == typeof(VkvIgnoreAttribute))
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
                                  x.AttributeType == typeof(VkvIgnoreAttribute))
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
