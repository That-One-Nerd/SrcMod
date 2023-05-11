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
        if (!File.Exists(dir + "\\GameInfo.txt")) return null;

        Mod mod = new()
        {
            Name = dir.Split("\\").Last(),
            RootDirectory = dir
        };

        return mod;
    }

    public override string ToString() => Name;
}
