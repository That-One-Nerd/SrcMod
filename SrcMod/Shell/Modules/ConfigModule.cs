namespace SrcMod.Shell.Modules;

[Module("config")]
public static class ConfigModule
{
    [Command("display")]
    [Command("list")]
    public static void DisplayConfig(ConfigDisplayMode mode = ConfigDisplayMode.All)
    {
        switch (mode)
        {
            case ConfigDisplayMode.Raw:
                DisplayConfigRaw();
                break;

            case ConfigDisplayMode.All:
                DisplayConfigAll();
                break;

            case ConfigDisplayMode.GameDirectories:
                DisplayConfigGameDirectories();
                break;

            case ConfigDisplayMode.RunUnsafeCommands:
                DisplayConfigUnsafeCommands();
                break;
        }
    }

    [Command("add")]
    [Command("append")]
    public static void AppendConfigVariable(string name, string value)
    {
        Config config = Config.LoadedConfig;

        switch (name.Trim().ToLower())
        {
            case "gamedirectories":
                config.GameDirectories = config.GameDirectories.Append(value).ToArray();
                break;

            case "rununsafecommands":
                throw new($"The config variable \"{name}\" is a single variable and cannot be appended to.");

            default: throw new($"Unknown config variable \"{name}\"");
        }

        Config.LoadedConfig = config;
    }

    [Command("delete")]
    [Command("remove")]
    public static void RemoveConfigVariable(string name, string value)
    {
        Config config = Config.LoadedConfig;

        switch (name.Trim().ToLower())
        {
            case "gamedirectories":
                config.GameDirectories = config.GameDirectories
                    .Where(x => x.Trim().ToLower() != value.Trim().ToLower())
                    .ToArray();
                break;

            case "rununsafecommands":
                throw new($"The config variable \"{name}\" is a single variable and cannot be appended to.");

            default: throw new($"Unknown config variable \"{name}\"");
        }

        Config.LoadedConfig = config;
    }

    [Command("reset")]
    public static void ResetConfig(string name = "all")
    {
        Config config = Config.LoadedConfig;

        switch (name.Trim().ToLower())
        {
            case "gamedirectories":
                config.GameDirectories = Config.Defaults.GameDirectories;
                break;

            case "rununsafecommands":
                config.RunUnsafeCommands = Config.Defaults.RunUnsafeCommands;
                break;

            case "all":
                config = Config.Defaults;
                break;

            default: throw new($"Unknown config variable \"{name}\"");
        }

        Config.LoadedConfig = config;
    }

    [Command("set")]
    public static void SetConfigVariable(string name, string value)
    {
        Config config = Config.LoadedConfig;

        switch (name.Trim().ToLower())
        {
            case "gamedirectories":
                throw new($"The config variable \"{name}\" is a list and must be added or removed to.");

            case "rununsafecommands":
                if (int.TryParse(value, out int intRes))
                {
                    AskMode mode = (AskMode)intRes;
                    if (!Enum.IsDefined(mode)) throw new($"(AskMode){value} is not a valid AskMode.");
                    config.RunUnsafeCommands = mode;
                }
                else if (Enum.TryParse(value, true, out AskMode modeRes))
                {
                    if (!Enum.IsDefined(modeRes)) throw new($"\"{value}\" is not a valid AskMode.");
                    config.RunUnsafeCommands = modeRes;
                }
                else throw new($"\"{value}\" is not a valid AskMode.");
                break;

            default: throw new($"Unknown config variable \"{name}\"");
        }

        Config.LoadedConfig = config;
    }

    private static void DisplayConfigAll()
    {
        DisplayConfigGameDirectories();
        DisplayConfigUnsafeCommands();
    }
    private static void DisplayConfigRaw()
    {
        // This is definitely a bit inefficient, but shouldn't be too much of an issue.

        MemoryStream ms = new();
        StreamWriter writer = new(ms, leaveOpen: true);
        JsonTextWriter jsonWriter = new(writer);

        Serializer.Serialize(jsonWriter, Config.LoadedConfig);

        jsonWriter.Close();
        writer.Close();
        ms.Position = 0;

        StreamReader reader = new(ms);
        string msg = reader.ReadToEnd();

        Write(msg);

        reader.Close();
        ms.Close();
    }
    private static void DisplayConfigGameDirectories()
    {
        Write("Steam Game Directories: ", null, false);
        if (Config.LoadedConfig.GameDirectories is null || Config.LoadedConfig.GameDirectories.Length <= 0)
            Write("None", ConsoleColor.DarkGray);
        else
        {
            Write("[", ConsoleColor.DarkGray);
            for (int i = 0; i < Config.LoadedConfig.GameDirectories.Length; i++)
            {
                Write("    \"", ConsoleColor.DarkGray, false);
                Write(Config.LoadedConfig.GameDirectories[i], ConsoleColor.White, false);
                if (i < Config.LoadedConfig.GameDirectories.Length - 1) Write("\",", ConsoleColor.DarkGray);
                else Write("\"", ConsoleColor.DarkGray);
            }
            Write("]", ConsoleColor.DarkGray);
        }
    }
    private static void DisplayConfigUnsafeCommands()
    {
        Write("Run Unsafe Commands: ", null, false);
        ConsoleColor color = Config.LoadedConfig.RunUnsafeCommands switch
        {
            AskMode.Never => ConsoleColor.Red,
            AskMode.Always => ConsoleColor.Green,
            AskMode.Ask or _ => ConsoleColor.DarkGray
        };
        Write(Config.LoadedConfig.RunUnsafeCommands, color);
    }

    public enum ConfigDisplayMode
    {
        Raw,
        All,
        GameDirectories,
        RunUnsafeCommands
    }
}
