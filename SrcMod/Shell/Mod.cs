namespace SrcMod.Shell;

public class Mod
{
    public string Name { get; set; }
    public string RootDirectory { get; set; }

    private Mod()
    {
        Name = string.Empty;
        RootDirectory = string.Empty;
    }

    public static Mod? ReadDirectory(string dir)
    {
        dir = dir.Trim().Replace('/', '\\');
        string check = dir;

        while (!string.IsNullOrEmpty(check))
        {
            if (File.Exists(Path.Combine(check, "GameInfo.txt")))
            {
                // Root mod directory found, go from here.
                // TODO: Parse VKV out of GameInfo.txt

                Mod mod = new()
                {
                    Name = Path.GetFileNameWithoutExtension(check), // TODO: replace with GameInfo: Title
                    RootDirectory = check
                };
                return mod;
            }

            check = Path.GetDirectoryName(check) ?? string.Empty; // Go to parent folder.
        }

        return null;
    }

    public override string ToString() => Name;
}
