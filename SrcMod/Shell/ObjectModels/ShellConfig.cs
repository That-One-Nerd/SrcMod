namespace SrcMod.Shell.ObjectModels;

public class ShellConfig
{
    public const string FilePath = "config.json";

    public static ShellConfig Defaults => new()
    {
        SteamDirectories = new[]
        {
            "Temp"
        }
    };
    public static ShellConfig LoadedConfig => p_data ?? Defaults;

    private static ShellConfig? p_data;

    public string[] SteamDirectories;

    public static void LoadConfig(string basePath)
    {
        string fullPath = Path.Combine(basePath, FilePath);

        if (!File.Exists(fullPath))
        {
            p_data = null;
            return;
        }
        StreamReader reader = new(fullPath);
        JsonTextReader jsonReader = new(reader);
        p_data = Serializer.Deserialize<ShellConfig>(jsonReader);
        jsonReader.Close();
        reader.Close();
    }

    public static void SaveConfig(string basePath)
    {
        string fullPath = Path.Combine(basePath, FilePath);

        StreamWriter writer = new(fullPath);
        JsonTextWriter jsonWriter = new(writer);
        Serializer.Serialize(jsonWriter, p_data);
        jsonWriter.Close();
        writer.Close();
    }
}
