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
            string gameInfoPath = Path.Combine(check, "GameInfo.txt");
            if (File.Exists(gameInfoPath))
            {
                // Root mod directory found, go from here.

                FileStream fs = new(gameInfoPath, FileMode.Open);
                GameInfo? modInfo = SerializeVkv.Deserialize<GameInfo>(fs);
                if (modInfo is null) continue;

                Mod mod = new()
                {
                    Name = modInfo.Title,
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
