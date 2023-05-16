namespace Valve.Vkv;

public record class VkvOptions
{
    public static VkvOptions Default => new();

    public bool closeWhenFinished;
    public int indentSize;
    public bool noExceptions;
    public bool resetStreamPosition;
    public bool serializeProperties;
    public SpacingMode spacing;
    public bool useEscapeCodes;
    public bool useQuotes;

    public VkvOptions()
    {
        closeWhenFinished = true;
        indentSize = 4;
        noExceptions = false;
        resetStreamPosition = false;
        serializeProperties = true;
        spacing = SpacingMode.DoubleTab;
        useEscapeCodes = false;
        useQuotes = false;
    }
}
