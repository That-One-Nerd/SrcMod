namespace SrcMod.Shell.Modules;

[Module("config")]
public static class ConfigModule
{
    [Command("display")]
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
