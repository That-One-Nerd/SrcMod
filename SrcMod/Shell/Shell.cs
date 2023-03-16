namespace SrcMod.Shell;

public class Shell
{
    public const string Author = "That_One_Nerd";
    public const string Name = "SrcMod";
    public const string Version = "Alpha 0.1.1";

    public readonly string? ShellDirectory;

    public List<CommandInfo> LoadedCommands;
    public List<ModuleInfo> LoadedModules;

    public Game? ActiveGame;
    public Mod? ActiveMod;
    public List<HistoryItem> History;
    public string WorkingDirectory;

    public Shell()
    {
        Console.CursorVisible = false;

        // Get shell directory and compare it to the path variable.
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyPath = assembly.Location;

        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath)) ShellDirectory = null;

        ShellDirectory = Path.GetDirectoryName(assemblyPath)!.Replace("/", "\\");
        if (ShellDirectory is null) Write("[ERROR] There was a problem detecting the shell's location. " +
                                          "Many featues will be disabled.", ConsoleColor.Red);

        // Check if the path in the PATH variable is correct.
        string envVal = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)!;
        string[] pathParts = envVal.Split(";");
        if (ShellDirectory is not null && !pathParts.Contains(ShellDirectory))
        {
            envVal += $"{ShellDirectory};";
            Environment.SetEnvironmentVariable("PATH", envVal, EnvironmentVariableTarget.User);
            Write($"[WARNING] The environment PATH does not contain the {Name} directory. It has now been added " +
                   "automatically. Is this your first time running the shell?", ConsoleColor.DarkYellow);
        }

        WorkingDirectory = Directory.GetCurrentDirectory();

        // Load modules and commands.
        List<Assembly?> possibleAsms = new()
        {
            assembly,
            Assembly.GetEntryAssembly(),
            Assembly.GetCallingAssembly()
        };

        LoadedModules = new();
        LoadedCommands = new();

        List<Type> possibleModules = new();
        foreach (Assembly? a in possibleAsms)
        {
            if (a is null) continue;
            possibleModules.AddRange(a.GetTypes().Where(x => !possibleModules.Contains(x)));
        }
        foreach (Type t in possibleModules)
        {
            ModuleInfo? module = ModuleInfo.FromModule(t);
            if (module is not null)
            {
                LoadedModules.Add(module);
                LoadedCommands.AddRange(module.Commands);
            }
        }

        // Other stuff
        History = new();

        // Send welcome message.
        Write("\nWelcome to ", ConsoleColor.White, false);
        Write($"{Name} {Version}", ConsoleColor.DarkCyan, false);
        Write(" by ", ConsoleColor.White, false);
        Write($"{Author}", ConsoleColor.DarkYellow);

        ActiveGame = null;

        ReloadDirectoryInfo();
    }

    public void AddHistory(HistoryItem item) => History.Add(item);
    public void UndoItem()
    {
        HistoryItem item = History.Last();
        item.Invoke();
        History.RemoveAt(History.Count - 1);
        Write($"Undid \"", newLine: false);
        Write(item.name, ConsoleColor.White, false);
        Write("\"");
    }

    public void UpdateWorkingDirectory(string dir)
    {
        string global = Path.GetFullPath(dir.Replace("/", "\\"), WorkingDirectory);

        Directory.SetCurrentDirectory(global);
        WorkingDirectory = global;
        ReloadDirectoryInfo();
    }

    public string ReadLine()
    {
        Console.CursorVisible = true;

        Write($"\n{WorkingDirectory}", ConsoleColor.DarkGreen, false);
        if (ActiveGame is not null) Write($" {ActiveGame}", ConsoleColor.DarkYellow, false);
        if (ActiveMod is not null) Write($" {ActiveMod}", ConsoleColor.Magenta, false);
        Write(null);

        Write($" {Name}", ConsoleColor.DarkCyan, false);
        Write(" > ", ConsoleColor.White, false);

        Console.ForegroundColor = ConsoleColor.White;
        string message = Console.ReadLine()!;
        Console.ResetColor();

        Console.CursorVisible = false;

        return message;
    }

    public void InvokeCommand(string cmd)
    {
        List<string> parts = new();
        string active = string.Empty;

        bool inQuotes = false;
        for (int i = 0; i < cmd.Length; i++)
        {
            char c = cmd[i];

            if (c == '\"' && i > 0 && cmd[i - 1] != '\\') inQuotes = !inQuotes;
            else if (c == ' ' && !inQuotes)
            {
                if (string.IsNullOrWhiteSpace(active)) continue;
                if (active.StartsWith('\"') && active.EndsWith('\"')) active = active[1..^1];
                parts.Add(active);
                active = string.Empty;
            }
            else active += c;
        }
        if (!string.IsNullOrWhiteSpace(active))
        {
            if (active.StartsWith('\"') && active.EndsWith('\"')) active = active[1..^1];
            parts.Add(active);
        }

        if (parts.Count < 1) return;

        string moduleName = parts[0].Trim().ToLower();
        foreach (ModuleInfo module in LoadedModules)
        {
            if (module.NameIsPrefix && module.NameId.Trim().ToLower() != moduleName) continue;
            string commandName;
            if (module.NameIsPrefix)
            {
                if (parts.Count < 2) continue;
                commandName = parts[1].Trim().ToLower();
            }
            else commandName = moduleName;

            foreach (CommandInfo command in module.Commands)
            {
                if (command.NameId.Trim().ToLower() != commandName) continue;
                int start = module.NameIsPrefix ? 2 : 1;
                string[] args = parts.GetRange(start, parts.Count - start).ToArray();
                
                try
                {
                    command.Invoke(args);
                }
                catch (TargetInvocationException ex)
                {
                    Write($"[ERROR] {ex.InnerException!.Message}", ConsoleColor.Red);
                    if (LoadingBarEnabled) LoadingBarEnd();
                }
                catch (Exception ex)
                {
                    Write($"[ERROR] {ex.Message}", ConsoleColor.Red);
                    if (LoadingBarEnabled) LoadingBarEnd();
                }
                return;
            }
        }

        Write($"[ERROR] Could not find command \"{cmd}\".", ConsoleColor.Red);
    }

    public void ReloadDirectoryInfo()
    {
        ActiveMod = Mod.ReadDirectory(WorkingDirectory);

        // Update title.
        string title = "SrcMod";
        if (ActiveMod is not null) title += $" - {ActiveMod.Name}";
        Console.Title = title;
    }
}
