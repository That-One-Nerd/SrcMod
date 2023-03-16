namespace SrcMod.Shell.Modules.ObjectModels;

public class CommandInfo
{
    public required ModuleInfo Module { get; init; }
    public required MethodInfo Method { get; init; }
    public string Name { get; private set; }
    public string NameId { get; private set; }
    public ParameterInfo[] Parameters { get; private set; }
    public int RequiredParameters { get; private set; }

    private CommandInfo()
    {
        Name = string.Empty;
        NameId = string.Empty;
        Parameters = Array.Empty<ParameterInfo>();
        RequiredParameters = 0;
    }

    public static CommandInfo? FromMethod(ModuleInfo parentModule, MethodInfo info)
    {
        CommandAttribute? attribute = info.GetCustomAttribute<CommandAttribute>();
        if (attribute is null) return null;

        if (info.ReturnType != typeof(void)) return null;
        ParameterInfo[] param = info.GetParameters();

        int required = 0;
        while (required < param.Length && !param[required].IsOptional) required++;

        return new()
        {
            Method = info,
            Module = parentModule,
            Name = info.Name,
            NameId = attribute.NameId,
            Parameters = param,
            RequiredParameters = required
        };
    }

    public void Invoke(params string[] args)
    {
        if (args.Length < RequiredParameters) throw new("Too few arguments. You must supply at least " +
                                                       $"{RequiredParameters}.");
        if (args.Length > Parameters.Length) throw new("Too many parameters. You must supply no more than " +
                                                      $"{Parameters.Length}.");

        object?[] invokes = new object?[Parameters.Length];
        for (int i = 0; i < invokes.Length; i++)
        {
            if (i < args.Length)
            {
                string msg = args[i];
                Type paramType = Parameters[i].ParameterType;
                object? val = TypeParsers.ParseAll(msg);
                if (val is string && paramType.IsEnum)
                {
                    if (Enum.TryParse(paramType, msg, true, out object? possible)) val = possible; 
                }
                val = Convert.ChangeType(val, paramType);
                invokes[i] = val;
            }
            else invokes[i] = Parameters[i].DefaultValue;
        }

        Method.Invoke(Module.Instance, invokes);
    }
}
