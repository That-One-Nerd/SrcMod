namespace SrcMod.Shell.Valve;

public record class VdfOptions
{
    public static VdfOptions Default => new();

    public bool closeWhenFinished;
    public int indentSize;
    public bool useEscapeCodes;
    public bool useQuotes;

    public VdfOptions()
    {
        closeWhenFinished = true;
        indentSize = 4;
        useEscapeCodes = false;
        useQuotes = false;
    }
}
