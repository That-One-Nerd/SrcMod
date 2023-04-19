﻿namespace SrcMod.Shell.Modules;

[Module("config")]
public static class ConfigModule
{
    [Command("display")]
    [Command("list")]
    public static void DisplayConfig(string display = "all")
    {
        switch (display.Trim().ToLower())
        {
            case "all":
                DisplayConfigAll();
                break;

            case "raw":
                DisplayConfigRaw();
                break;

            default:
                DisplayConfigName(display);
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
        FieldInfo[] validFields = (from field in typeof(Config).GetFields()
                                   let isPublic = field.IsPublic
                                   let isStatic = field.IsStatic
                                   where isPublic && !isStatic
                                   select field).ToArray();

        foreach (FieldInfo field in validFields)
            DisplayConfigItem(field.GetValue(Config.LoadedConfig), name: field.Name);
    }
    private static void DisplayConfigItem<T>(T item, int indents = 0, string name = "", bool newLine = true)
    {
        Write(new string(' ', indents * 4), newLine: false);
        if (!string.IsNullOrWhiteSpace(name)) Write($"{name}: ", newLine: false);

        if (item is Array itemArray)
        {
            if (itemArray.Length < 1)
            {
                Write("[]", ConsoleColor.DarkGray, newLine);
                return;
            }

            Write("[", ConsoleColor.DarkGray);
            for (int i = 0; i < itemArray.Length; i++)
            {
                DisplayConfigItem(itemArray.GetValue(i), indents + 1, newLine: false);
                if (i < itemArray.Length - 1) Write(',', newLine: false);
                Write('\n', newLine: false);
            }
            Write(new string(' ', indents * 4) + "]", ConsoleColor.DarkGray, newLine);
        }
        else if (item is byte itemByte) Write($"0x{itemByte:X2}", ConsoleColor.Yellow, newLine);
        else if (item is sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
            Write(item, ConsoleColor.Yellow, newLine);
        else if (item is bool itemBool) Write(item, itemBool ? ConsoleColor.Green : ConsoleColor.Red, newLine);
        else if (item is char)
        {
            Write("\'", ConsoleColor.DarkGray, false);
            Write(item, ConsoleColor.Blue, false);
            Write("\'", ConsoleColor.DarkGray, newLine);
        }
        else if (item is string)
        {
            Write("\"", ConsoleColor.DarkGray, false);
            Write(item, ConsoleColor.DarkCyan, false);
            Write("\"", ConsoleColor.DarkGray, newLine);
        }
        else if (item is AskMode) Write(item, item switch
        {
            AskMode.Never => ConsoleColor.Red,
            AskMode.Always => ConsoleColor.Green,
            AskMode.Ask or _ => ConsoleColor.DarkGray
        }, newLine);
        else Write(item, newLine: newLine);
    }
    private static void DisplayConfigName(string name)
    {
        FieldInfo[] validFields = (from field in typeof(Config).GetFields()
                                   let isPublic = field.IsPublic
                                   let isStatic = field.IsStatic
                                   where isPublic && !isStatic
                                   select field).ToArray();

        FieldInfo? chosenField = validFields.FirstOrDefault(x => x.Name.Trim().ToLower() == name.Trim().ToLower());
        if (chosenField is null) throw new($"No config variable named \"{name}\".");

        DisplayConfigItem(chosenField.GetValue(Config.LoadedConfig), name: chosenField.Name);
    }
    private static void DisplayConfigRaw()
    {
        string json = JsonConvert.SerializeObject(Config.LoadedConfig, Formatting.Indented);
        Write(json);
    }
}
