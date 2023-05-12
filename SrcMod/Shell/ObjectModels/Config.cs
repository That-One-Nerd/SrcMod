namespace SrcMod.Shell.ObjectModels;

public class Config
{
    public const string FilePath = "config.json";

    public static Config Defaults => new();

    private static readonly FieldInfo[] p_configSharedFields;
    private static readonly FieldInfo[] p_changeSharedFields;

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

    // These variables should only exist in the Config class so they aren't marked as shared.
    private readonly string p_steamLocation;

    static Config()
    {
        // Generate shared fields between the config class and its changes equivalent.
        p_applied = Defaults;

        FieldInfo[] configFields = (from field in typeof(Config).GetFields()
                                    let isPublic = field.IsPublic
                                    let isStatic = field.IsStatic
                                    where isPublic && !isStatic
                                    select field).ToArray(),
                    changeFields = (from field in typeof(Changes).GetFields()
                                    let isPublic = field.IsPublic
                                    let isStatic = field.IsStatic
                                    where isPublic && !isStatic
                                    select field).ToArray();

        List<FieldInfo> sharedConfigFields = new(),
                        sharedChangeFields = new();
        foreach (FieldInfo field in configFields)
        {
            FieldInfo? changeEquivalent = changeFields.FirstOrDefault(
                x => x.Name == field.Name &&
                (x.FieldType == field.FieldType || Nullable.GetUnderlyingType(x.FieldType) == field.FieldType));

            if (changeEquivalent is null) continue;

            sharedConfigFields.Add(field);
            sharedChangeFields.Add(changeEquivalent);
        }

        static int sortByName(FieldInfo a, FieldInfo b) => a.Name.CompareTo(b.Name);

        sharedConfigFields.Sort(sortByName);
        sharedChangeFields.Sort(sortByName);

        p_configSharedFields = sharedConfigFields.ToArray();
        p_changeSharedFields = sharedChangeFields.ToArray();
    }

    public string[] GameDirectories;
    public AskMode RunUnsafeCommands;

    internal Config()
    {
        // Locate some steam stuff.
        const string steamLocationKey = @"Software\Valve\Steam";
        RegistryKey? key = Registry.CurrentUser.OpenSubKey(steamLocationKey);
        if (key is null)
        {
            Write("[FATAL] Cannot locate Steam installation. Do you have Steam installed?",
                ConsoleColor.DarkRed);
            Thread.Sleep(1000);
            BaseModule.QuitShell(-1);

            // This should never run, and is just here to supress
            // a couple compiler warnings.
            p_steamLocation = string.Empty;
            GameDirectories = Array.Empty<string>();
            RunUnsafeCommands = AskMode.Ask;
            return;
        }
        p_steamLocation = (string)key.GetValue("SteamPath")!;

        // Assign config variables.

        string gameDirDataPath = Path.Combine(p_steamLocation, @"steamapps\libraryfolders.vdf");

        FileStream gameDirData = new(gameDirDataPath, FileMode.Open);
        LibraryFolder[]? folders = SerializeVkv.Deserialize<LibraryFolder[]>(gameDirData);
        if (folders is null)
        {
            Write("[WARNING] Error parsing Steam game directories.", ConsoleColor.DarkYellow);
            GameDirectories = Array.Empty<string>();
        }
        else
        {
            GameDirectories = new string[folders.Length];
            for (int i = 0; i < folders.Length; i++) GameDirectories[i] = folders[i].path;
        }

        RunUnsafeCommands = AskMode.Ask;
    }

    public Config ApplyChanges(Changes changes)
    {
        for (int i = 0; i < p_configSharedFields.Length; i++) 
        {
            FieldInfo configField = p_configSharedFields[i],
                      changeField = p_changeSharedFields[i];

            object? toChange = changeField.GetValue(changes);

            if (toChange is null) continue;

            if (configField.FieldType.IsArray)
            {
                object[] currentArray = ((Array)configField.GetValue(this)!).CastArray<object>(),
                         changeArray = ((Array)toChange).CastArray<object>();

                currentArray = currentArray.Union(changeArray).ToArray();
                configField.SetValue(this, currentArray.CastArray(configField.FieldType.GetElementType()!));
            }
            else configField.SetValue(this, toChange);
        }

        return this;
    }
    public Changes GetChanges(Config? reference = null)
    {
        reference ??= Defaults;
        Changes changes = new();

        for (int i = 0; i < p_configSharedFields.Length; i++)
        {
            FieldInfo configField = p_configSharedFields[i],
                      changeField = p_changeSharedFields[i];

            object? toSet = configField.GetValue(this);

            if (toSet is null) continue;

            if (configField.FieldType.IsArray)
            {
                object[] configArray = ((Array)toSet).CastArray<object>(),
                         referenceArray = ((Array)configField.GetValue(Defaults)!).CastArray<object>(),
                         changesArray = configArray.Where(x => !referenceArray.Contains(x)).ToArray();
                changeField.SetValue(changes, changesArray.CastArray(configField.FieldType.GetElementType()!));
            }
            else changeField.SetValue(changes, toSet);
        }

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
        p_changes = Tools.SerializerJson.Deserialize<Changes?>(jsonReader);
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
        Tools.SerializerJson.Serialize(jsonWriter, p_changes);
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
