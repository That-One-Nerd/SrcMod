namespace Valve.Vkv;

public record class VkvOptions
{
    public static VkvOptions Default => new();

    public bool closeWhenFinished;
    public int indentSize;
    public bool resetStreamPosition;
    public bool serializeProperties;
    public bool useEscapeCodes;
    public bool useQuotes;

    public VkvOptions()
    {
        closeWhenFinished = true;
        indentSize = 4;
        resetStreamPosition = false;
        serializeProperties = true;
        useEscapeCodes = false;
        useQuotes = false;
    }
}
