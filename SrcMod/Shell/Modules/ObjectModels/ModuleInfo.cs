﻿namespace SrcMod.Shell.Modules.ObjectModels;

public class ModuleInfo
{
    public List<CommandInfo> Commands { get; init; }
    public object? Instance { get; init; }
    public string Name { get; init; }
    public string NameId { get; init; }
    public bool NameIsPrefix { get; init; }
    public required Type Type { get; init; }

    private ModuleInfo()
    {
        Commands = new();
        Instance = null;
        Name = string.Empty;
        NameId = string.Empty;
        NameIsPrefix = true;
    }

    public static ModuleInfo? FromModule(Type info)
    {
        ModuleAttribute? attribute = info.GetCustomAttribute<ModuleAttribute>();
        if (attribute is null) return null;

        object? instance = info.IsAbstract ? null : Activator.CreateInstance(info);

        ModuleInfo module = new()
        {
            Instance = instance,
            Name = info.Name,
            NameId = attribute.NameId,
            NameIsPrefix = attribute.NameIsPrefix,
            Type = info
        };

        List<CommandInfo> commands = new();
        foreach (MethodInfo method in info.GetMethods())
        {
            CommandInfo? cmd = CommandInfo.FromMethod(module, method);
            if (cmd is null) continue;
            commands.Add(cmd);
        }

        module.Commands.AddRange(commands);

        return module;
    }
}
