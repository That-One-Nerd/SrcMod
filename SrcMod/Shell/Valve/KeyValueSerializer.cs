namespace SrcMod.Shell.Valve;

public class KeyValueSerializer
{
    public int? IndentSize => p_options.indentSize;
    public bool UseEscapeCodes => p_options.useEscapeCodes;
    public bool UseNameQuotes => p_options.useNameQuotes;
    public bool UseValueQuotes => p_options.useValueQuotes;

    private readonly Options p_options;

    public KeyValueSerializer() : this(Options.Default) { }
    public KeyValueSerializer(Options options)
    {
        p_options = options;
    }

    public StringBuilder Serialize(StringBuilder builder, KeyValueNode parentNode, string? parentNodeName = null)
    {
        if (parentNodeName is not null) builder.AppendLine(KeyValueConvert.SerializeName(parentNodeName, p_options));
        return builder;
    }

    public record class Options
    {
        public static Options Default => new();

        public int? indentSize;
        public bool useEscapeCodes;
        public bool useNameQuotes;
        public bool useValueQuotes;

        public Options()
        {
            indentSize = null;
            useEscapeCodes = false;
            useNameQuotes = true;
            useValueQuotes = false;
        }
    }
}
