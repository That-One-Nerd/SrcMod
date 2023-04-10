namespace SrcMod.Shell;

public class Shell
{
    public const string Author = "That_One_Nerd";
    public const string Name = "SrcMod";
    public const string Version = "Alpha 0.3.3";

    public readonly string? ShellDirectory;

    public List<CommandInfo> LoadedCommands;
    public List<ModuleInfo> LoadedModules;

    public Game? ActiveGame;
    public Mod? ActiveMod;
    public List<HistoryItem> History;
    public string WorkingDirectory;

    private bool lastCancel;
    private bool printedCancel;

    private BackgroundWorker? activeCommand;

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

        // Load config.
        if (ShellDirectory is null) Write("[WARNING] Could not load config from shell location. Defaults will be used.");
        else ShellConfig.LoadConfig(ShellDirectory);

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
            ModuleInfo? module = ModuleInfo.FromType(t);
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

        lastCancel = false;
        activeCommand = null;
        Console.CancelKeyPress += HandleCancel;

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
        Write($"\n{WorkingDirectory}", ConsoleColor.DarkGreen, false);
        if (ActiveGame is not null) Write($" {ActiveGame}", ConsoleColor.DarkYellow, false);
        if (ActiveMod is not null) Write($" {ActiveMod}", ConsoleColor.Magenta, false);
        Write(null);

        Write($" {Name}", ConsoleColor.DarkCyan, false);
        Write(" > ", ConsoleColor.White, false);

        bool printed = false;

        if (lastCancel && !printedCancel)
        {
            // Print the warning. A little bit of mess because execution must
            // continue without funny printing errors but it's alright I guess.

            int originalLeft = Console.CursorLeft;

            Console.CursorTop -= 3;
            Write("Press ^C again to exit the shell.", ConsoleColor.Red);
            PlayWarningSound();

            printedCancel = true;
            Console.CursorTop += 2;

            Console.CursorLeft = originalLeft;
            printed = true;
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.CursorVisible = true;
        string message = Console.ReadLine()!;
        Console.CursorVisible = false;
        Console.ResetColor();

        if (!printed)
        {
            lastCancel = false;
            printedCancel = false;
        }

        return message;
    }

    public void InvokeCommand(string cmd)
    {
        if (cmd is null)
        {
            // This usually won't happen, but might if for example
            // the shell cancel interrupt is called. This probably
            // happens for other shell interrupts are called.
            Write(null);
            return;
        }

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

                void runCommand(object? sender, DoWorkEventArgs e)
                {
#if RELEASE
                    try
                    {
#endif
                        command.Invoke(args);
#if RELEASE
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
#endif
                }

                activeCommand = new();
                activeCommand.DoWork += runCommand;
                activeCommand.RunWorkerAsync();

                activeCommand.WorkerSupportsCancellation = command.CanBeCancelled;

                while (activeCommand is not null && activeCommand.IsBusy) Thread.Yield();

                if (activeCommand is not null)
                {
                    activeCommand.Dispose();
                    activeCommand = null;
                }

                if (ShellDirectory is null) Write("[WARNING] Could not save config to shell location. Any changes will be ignored.");
                else ShellConfig.SaveConfig(ShellDirectory);
                return;
            }
        }

        Write($"[ERROR] Could not find command \"{cmd}\".", ConsoleColor.Red);
    }

    private static void PlayErrorSound()
    {
        Winmm.PlaySound("SystemHand", nint.Zero,
                (uint)(Winmm.PlaySoundFlags.Alias | Winmm.PlaySoundFlags.Async));
    }
    private static void PlayWarningSound()
    {
        Winmm.PlaySound("SystemAsterisk", nint.Zero,
                (uint)(Winmm.PlaySoundFlags.Alias | Winmm.PlaySoundFlags.Async));
    }

    public void ReloadDirectoryInfo()
    {
        ActiveMod = Mod.ReadDirectory(WorkingDirectory);

        // Update title.
        string title = "SrcMod";
        if (ActiveMod is not null) title += $" - {ActiveMod.Name}";
        Console.Title = title;
    }

    private void HandleCancel(object? sender, ConsoleCancelEventArgs args)
    {
        if (activeCommand is not null && activeCommand.IsBusy)
        {
            if (activeCommand.WorkerSupportsCancellation)
            {
                // Kill the active command.
                activeCommand.CancelAsync();
                activeCommand.Dispose();
                activeCommand = null;
            }
            else
            {
                // Command doesn't support cancellation.
                // Warn the user.
                PlayErrorSound();
            }

            lastCancel = false;
            printedCancel = false;
            args.Cancel = true;
            return;
        }

        // Due to some funny multithreading issues, we want to make the warning label
        // single-threaded on the shell.
        if (!lastCancel)
        {
            // Enable the warning. The "ReadLine" method will do the rest.
            lastCancel = true;
            args.Cancel = true; // "Cancel" referring to the cancellation of the cancel operation.
            return;
        }

        // Actually kill the shell. We do still have to worry about some multithreaded
        // nonsense, but a bearable amount of it.
        Console.ResetColor();
        Environment.Exit(0);
    }
}
