namespace SrcMod.Shell.ObjectModels;

public class Config
{
    public const string FilePath = "config.json";

    public static Config Defaults => new();

    public static Config LoadedConfig
    {
        get => p_applied;
        set
        {
            p_applied = value;
            UpdateChanges();
        }
    }

    private static Config p_applied;
    private static Changes? p_changes;

    static Config()
    {
        p_applied = Defaults;
    }

    public string[] GameDirectories;
    public AskMode RunUnsafeCommands;

    internal Config()
    {
        GameDirectories = Array.Empty<string>();
        RunUnsafeCommands = AskMode.Ask;
    }

    public Config ApplyChanges(Changes changes)
    {
        if (changes.GameDirectories is not null)
            GameDirectories = GameDirectories.Union(changes.GameDirectories).ToArray();

        if (changes.RunUnsafeCommands is not null) RunUnsafeCommands = changes.RunUnsafeCommands.Value;

        return this;
    }
    public Changes GetChanges(Config? baseConfig = null)
    {
        Config reference = baseConfig ?? Defaults;
        Changes changes = new()
        {
            GameDirectories = reference.GameDirectories == GameDirectories ? null :
                GameDirectories.Where(x => !reference.GameDirectories.Contains(x)).ToArray(),
            RunUnsafeCommands = reference.RunUnsafeCommands == RunUnsafeCommands ? null : RunUnsafeCommands
        };

        return changes;
    }

    public static void LoadConfig(string basePath)
    {
        string fullPath = Path.Combine(basePath, FilePath);

        if (!File.Exists(fullPath))
        {
            p_applied = Defaults;
            p_changes = null;
            return;
        }
        StreamReader reader = new(fullPath);
        JsonTextReader jsonReader = new(reader);
        p_changes = Serializer.Deserialize<Changes?>(jsonReader);
        jsonReader.Close();
        reader.Close();

        p_applied = p_changes is null ? Defaults : Defaults.ApplyChanges(p_changes);
    }
    public static void SaveConfig(string basePath)
    {
        string fullPath = Path.Combine(basePath, FilePath);

        if (p_changes is null || !p_changes.Any())
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
            return;
        }

        StreamWriter writer = new(fullPath);
        JsonTextWriter jsonWriter = new(writer)
        {
            Indentation = 4
        };
        Serializer.Serialize(jsonWriter, p_changes);
        jsonWriter.Close();
        writer.Close();
    }

    public static void UpdateChanges()
    {
        p_changes = p_applied.GetChanges(Defaults);
    }

    public class Changes
    {
        public string[]? GameDirectories;
        public AskMode? RunUnsafeCommands;

        public bool Any() => typeof(Changes).GetFields().Any(x => x.GetValue(this) is not null);
    }
}
