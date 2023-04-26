namespace SrcMod.Shell.Valve;

public static class KeyValueConvert
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

    public static string SerializeName(string content, KeyValueSerializer.Options? options = null)
    {
        options ??= KeyValueSerializer.Options.Default;
        if (options.useNameQuotes) content = $"\"{content}\"";
        if (options.useEscapeCodes)
            foreach (KeyValuePair<string, string> escapeCode in p_escapeCodes)
                content = content.Replace(escapeCode.Key, escapeCode.Value);

        return content;
    }
}
