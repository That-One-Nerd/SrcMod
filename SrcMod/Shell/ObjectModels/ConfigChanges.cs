namespace SrcMod.Shell.ObjectModels;

public record struct ConfigChanges
{
    public bool HasChange => SteamDirectories is not null;

    public string[]? SteamDirectories;
}
