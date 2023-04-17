namespace SrcMod.Shell.Modules;

[Module("config")]
public static class ConfigModule
{
    [Command("display")]
    public static void DisplayConfig(ConfigDisplayMode mode = ConfigDisplayMode.Color)
    {
        switch (mode)
        {
            case ConfigDisplayMode.Raw:
                DisplayConfigRaw();
                break;

            case ConfigDisplayMode.Color:
                DisplayConfigColor();
                break;
        }
    }

    private static void DisplayConfigColor()
    {
        Config config = Config.LoadedConfig;

        Write("Steam Game Directories: ", null, false);
        if (config.GameDirectories is null || config.GameDirectories.Length <= 0) Write("None", ConsoleColor.DarkGray);
        else
        {
            Write("[", ConsoleColor.DarkGray);
            for (int i = 0; i < config.GameDirectories.Length; i++)
            {
                Write("    \"", ConsoleColor.DarkGray, false);
                Write(config.GameDirectories[i], ConsoleColor.White, false);
                if (i < config.GameDirectories.Length - 1) Write("\",", ConsoleColor.DarkGray);
                else Write("\"", ConsoleColor.DarkGray);
            }
            Write("]", ConsoleColor.DarkGray);
        }

        Write("Run Unsafe Commands: ", null, false);
        ConsoleColor color = config.RunUnsafeCommands switch
        {
            AskMode.Never => ConsoleColor.Red,
            AskMode.Always => ConsoleColor.Green,
            AskMode.Ask or _ => ConsoleColor.DarkGray
        };
        Write(config.RunUnsafeCommands, color);
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

    public enum ConfigDisplayMode
    {
        Raw,
        Color
    }
}
