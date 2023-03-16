namespace SrcMod.Shell;

public class Mod
{
    public string Name { get; set; }

    private Mod()
    {
        Name = string.Empty;
    }

    public static Mod? ReadDirectory(string dir)
    {
        if (!File.Exists(dir + "\\GameInfo.txt")) return null;

        Mod mod = new()
        {
            Name = dir.Split("\\").Last()
        };

        return mod;
    }

    public override string ToString() => Name;
}
