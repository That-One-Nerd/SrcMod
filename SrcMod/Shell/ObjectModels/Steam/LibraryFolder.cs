namespace SrcMod.Shell.ObjectModels.Steam;

public class LibraryFolder
{
    public string path;
    public Dictionary<int, ulong> apps;

    public LibraryFolder()
    {
        path = string.Empty;
        apps = new();
    }
}
