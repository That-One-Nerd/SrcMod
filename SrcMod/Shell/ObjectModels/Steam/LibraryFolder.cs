namespace SrcMod.Shell.ObjectModels.Steam;

public class LibraryFolder
{
    public string path;
    public Dictionary<int, int> apps;

    public LibraryFolder()
    {
        path = string.Empty;
        apps = new();
    }
}
