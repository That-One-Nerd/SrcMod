using SharpCompress;

namespace SrcMod.Shell.Modules;

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
        FieldInfo[] validFields = (from field in typeof(Config).GetFields()
                                   let isPublic = field.IsPublic
                                   let isStatic = field.IsStatic
                                   where isPublic && !isStatic
                                   select field).ToArray();

        FieldInfo? chosenField = validFields.FirstOrDefault(x => x.Name.Trim().ToLower() == name.Trim().ToLower());
        if (chosenField is null) throw new($"No valid config variable named \"{name}\".");
        else if (!chosenField.FieldType.IsArray) throw new($"The variable \"{chosenField.Name}\" is not an array" +
                                                            " and cannot have data added or removed from it." +
                                                            " Instead, set or reset the variable.");

        object parsed = TypeParsers.ParseAll(value);
        if (parsed is string parsedStr
            && chosenField.FieldType.IsEnum
            && Enum.TryParse(chosenField.FieldType, parsedStr, true, out object? obj)) parsed = obj;

        Type arrayType = chosenField.FieldType.GetElementType()!;

        Array arrayValue = (Array)chosenField.GetValue(Config.LoadedConfig)!;
        ArrayList collection = new(arrayValue) { parsed };

        chosenField.SetValue(Config.LoadedConfig, collection.ToArray()!.CastArray(arrayType));
        DisplayConfigItem(chosenField.GetValue(Config.LoadedConfig), name: chosenField.Name);
    }

    [Command("delete")]
    [Command("remove")]
    public static void RemoveConfigVariable(string name, string value)
    {
        FieldInfo[] validFields = (from field in typeof(Config).GetFields()
                                   let isPublic = field.IsPublic
                                   let isStatic = field.IsStatic
                                   where isPublic && !isStatic
                                   select field).ToArray();

        FieldInfo? chosenField = validFields.FirstOrDefault(x => x.Name.Trim().ToLower() == name.Trim().ToLower());
        if (chosenField is null) throw new($"No valid config variable named \"{name}\".");
        else if (!chosenField.FieldType.IsArray) throw new($"The variable \"{chosenField.Name}\" is not an array" +
                                                            " and cannot have data added or removed from it." +
                                                            " Instead, set or reset the variable.");

        object parsed = TypeParsers.ParseAll(value);
        if (parsed is string parsedStr
            && chosenField.FieldType.IsEnum
            && Enum.TryParse(chosenField.FieldType, parsedStr, true, out object? obj)) parsed = obj;

        Type arrayType = chosenField.FieldType.GetElementType()!;

        Array arrayValue = (Array)chosenField.GetValue(Config.LoadedConfig)!;
        ArrayList collection = new(arrayValue);
        if (!collection.Contains(parsed)) throw new($"The value \"{value}\" is not contained in this variable.");
        collection.Remove(parsed);

        chosenField.SetValue(Config.LoadedConfig, collection.ToArray()!.CastArray(arrayType));
        DisplayConfigItem(chosenField.GetValue(Config.LoadedConfig), name: chosenField.Name);
    }

    [Command("reset")]
    public static void ResetConfig(string name = "all")
    {
        switch (name.Trim().ToLower())
        {
            case "all":
                Config.LoadedConfig = Config.Defaults;
                DisplayConfig("all");
                break;

            default:
                ResetConfigVar(name);
                break;
        }
    }

    [Command("set")]
    public static void SetConfigVariable(string name, string value)
    {
        FieldInfo[] validFields = (from field in typeof(Config).GetFields()
                                   let isPublic = field.IsPublic
                                   let isStatic = field.IsStatic
                                   where isPublic && !isStatic
                                   select field).ToArray();

        FieldInfo? chosenField = validFields.FirstOrDefault(x => x.Name.Trim().ToLower() == name.Trim().ToLower());
        if (chosenField is null) throw new($"No valid config variable named \"{name}\".");
        else if (chosenField.FieldType.IsArray) throw new($"The variable \"{chosenField.Name}\" is an array and" +
                                                           " cannot be directly set. Instead, add or remove items" +
                                                           " from it.");

        object parsed = TypeParsers.ParseAll(value);
        if (parsed is string parsedStr
            && chosenField.FieldType.IsEnum
            && Enum.TryParse(chosenField.FieldType, parsedStr, true, out object? obj)) parsed = obj;

        chosenField.SetValue(Config.LoadedConfig, parsed);
        DisplayConfigItem(chosenField.GetValue(Config.LoadedConfig), name: chosenField.Name);
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

        if (item is null) Write("null", ConsoleColor.DarkRed, newLine);
        else if (item is Array itemArray)
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

    private static void ResetConfigVar(string name)
    {
        FieldInfo[] validFields = (from field in typeof(Config).GetFields()
                                   let isPublic = field.IsPublic
                                   let isStatic = field.IsStatic
                                   where isPublic && !isStatic
                                   select field).ToArray();

        FieldInfo? chosenField = validFields.FirstOrDefault(x => x.Name.Trim().ToLower() == name.Trim().ToLower());
        if (chosenField is null) throw new($"No valid config variable named \"{name}\".");

        chosenField.SetValue(Config.LoadedConfig, chosenField.GetValue(Config.Defaults));
        DisplayConfigItem(chosenField.GetValue(Config.LoadedConfig), name: chosenField.Name);
    }
}
