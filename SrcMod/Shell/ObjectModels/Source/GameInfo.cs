namespace SrcMod.Shell.ObjectModels.Source;

// Referencing https://developer.valvesoftware.com/wiki/Gameinfo.txt.
public class GameInfo
{
    // Name
    public string Game;
    public string Title;
    public string? Title2;
    public bool? GameLogo;

    // Options
    public string? Type;
    public bool? NoDifficulty;
    public bool? HasPortals;
    public bool? NoCrosshair;
    public bool? AdvCrosshair;
    public bool? NoModels;
    public bool? NoHIModel;

    public Dictionary<string, int>? Hidden_Maps;
    public Dictionary<string, string>? CommandLine;

    // Steam games list
    public string? Developer;
    public string? Developer_URL;
    public string? Manual;
    public string? Icon;

    // Engine and tools
    public bool? Nodegraph;
    public string? GameData;
    public string? InstancePath;
    public bool? SupportsDX8;
    public bool? SupportsVR;
    public bool? SupportsXBox360;
    public FileSystemData FileSystem;

    public GameInfo()
    {
        Game = string.Empty;
        Title = string.Empty;
        FileSystem = new();
    }

    public class FileSystemData
    {
        public int SteamAppID;
        public int? AdditionalContentId;
        public int? ToolsAppId;

        public Dictionary<string, string> SearchPaths;

        public FileSystemData()
        {
            SearchPaths = new();
        }
    }
}
