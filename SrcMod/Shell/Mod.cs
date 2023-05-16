namespace SrcMod.Shell;

public class Mod
{
    public Game BaseGame { get; set; }

    public string? Developer { get; set; }
    public string? DeveloperUrl { get; set; }
    public Dictionary<SearchPathType, string> SearchPaths { get; set; }
    public string? ManualUrl { get; set; }

    public string? FgdDataPath { get; set; }
    public string? IconPath { get; set; }
    public string? InstancePath { get; set; }

    public PlayerType PlayerMode { get; set; }

    public CrosshairFlags CrosshairMenuFlags { get; set; }
    public bool ShowDifficultyMenu { get; set; }
    public bool ShowModelMenu { get; set; }
    public bool ShowPortalMenu { get; set; }
    public SupportFlags SupportingFlags { get; set; }

    public bool HiResModels { get; set; }

    public string[] HiddenMaps { get; set; }

    public string Name { get; set; }
    public string? Motto { get; set; }
    public TitleDisplay TitleDisplayMode { get; set; }

    public bool BuildMapNodegraphs { get; set; }

    public Dictionary<string, string>? MapbaseLaunchOptions { get; set; }
    public string RootDirectory { get; set; }

    private Mod()
    {
        BaseGame = Game.Unknown;
        SearchPaths = new();
        HiddenMaps = Array.Empty<string>();
        Name = string.Empty;
        RootDirectory = string.Empty;
    }

    public static Mod FromInfo(string root, GameInfo info)
    {
        Mod curMod = new()
        {
            BaseGame = Game.FromSteamId(info.FileSystem.SteamAppID),
            BuildMapNodegraphs = info.Nodegraph is not null && info.Nodegraph.Value,
            CrosshairMenuFlags = CrosshairFlags.None,
            Developer = info.Developer,
            DeveloperUrl = info.Developer_URL,
            FgdDataPath = info.GameData,
            HiddenMaps = info.Hidden_Maps is null ? Array.Empty<string>() : info.Hidden_Maps.Keys.ToArray(),
            HiResModels = info.NoHIModel is null || !info.NoHIModel.Value,
            IconPath = info.Icon is null ? null : info.Icon.Trim().Replace('/', '\\') + ".tga",
            InstancePath = info.InstancePath,
            MapbaseLaunchOptions = info.CommandLine,
            ManualUrl = info.Manual,
            Motto = info.Title2,
            Name = string.IsNullOrEmpty(info.Title) ? "Default Mod" : info.Title,
            PlayerMode = info.Type is null ? PlayerType.Both : info.Type.Trim().ToLower() switch
            {
                "singleplayer_only" => PlayerType.Singleplayer,
                "multiplayer_only" => PlayerType.Multiplayer,
                _ => throw new ArgumentException($"Unknown type \"{info.Type}\"")
            },
            RootDirectory = root,
            SearchPaths = new(),
            ShowDifficultyMenu = info.NoDifficulty is null || !info.NoDifficulty.Value,
            ShowModelMenu = info.NoModels is null || !info.NoModels.Value,
            ShowPortalMenu = info.HasPortals is not null && info.HasPortals.Value,
            SupportingFlags = SupportFlags.None,
            TitleDisplayMode = info.GameLogo is null ? TitleDisplay.Title :
                (info.GameLogo.Value ? TitleDisplay.Logo : TitleDisplay.Title)
        };
        if (curMod.PlayerMode == PlayerType.Multiplayer && info.NoDifficulty is null)
            curMod.ShowDifficultyMenu = false;

        if (info.NoCrosshair is null || !info.NoCrosshair.Value)
            curMod.CrosshairMenuFlags |= CrosshairFlags.ShowMultiplayer;
        if (info.AdvCrosshair is not null && info.AdvCrosshair.Value)
            curMod.CrosshairMenuFlags |= CrosshairFlags.AdvancedMenu;

        if (info.SupportsDX8 is not null && info.SupportsDX8.Value)
            curMod.SupportingFlags |= SupportFlags.DirectX8;
        if (info.SupportsVR is not null && info.SupportsVR.Value)
            curMod.SupportingFlags |= SupportFlags.VirtualReality;
        if (info.SupportsXBox360 is not null && info.SupportsXBox360.Value)
            curMod.SupportingFlags |= SupportFlags.XBox360;

        foreach (KeyValuePair<string, string> pair in info.FileSystem.SearchPaths)
        {
            SearchPathType type = SearchPathType.Unknown;
            string[] parts = pair.Key.Trim().ToLower().Split('+');
            foreach (string part in parts) type |= part switch
            {
                "game" => SearchPathType.Game,
                "game_write" => SearchPathType.GameWrite,
                "gamebin" => SearchPathType.GameBinaries,
                "platform" => SearchPathType.Platform,
                "mod" => SearchPathType.Mod,
                "mod_write" => SearchPathType.ModWrite,
                "default_write_path" => SearchPathType.DefaultWritePath,
                "vpk" => SearchPathType.Vpk,
                _ => SearchPathType.Unknown
            };
        }

        return curMod;
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

                return FromInfo(check, modInfo);
            }

            check = Path.GetDirectoryName(check) ?? string.Empty; // Go to parent folder.
        }

        return null;
    }

    public override string ToString() => Name;

    [Flags]
    public enum CrosshairFlags
    {
        None = 0,
        ShowMultiplayer = 1,
        AdvancedMenu = 2
    }

    [Flags]
    public enum SupportFlags
    {
        None,
        DirectX8 = 1,
        VirtualReality = 2,
        XBox360 = 4
    }

    [Flags]
    public enum SearchPathType
    {
        Unknown = 0,
        Game = 1,
        GameWrite = 2,
        GameBinaries = 4,
        Platform = 8,
        Mod = 16,
        ModWrite = 32,
        DefaultWritePath = 64,
        Vpk = 128
    }

    public enum PlayerType
    {
        Singleplayer = 1,
        Multiplayer = 2,
        Both = Singleplayer | Multiplayer
    }
    public enum TitleDisplay
    {
        Title,
        Logo
    }
}
