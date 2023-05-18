namespace SrcMod.Shell;

public class Shell
{
    public const string Author = "That_One_Nerd";
    public const string Name = "SrcMod";
    public const string Version = "Beta 0.5.0";

    public bool HasAnyDisplayableError => HasDisplayableError || Config.HasDisplayableError;
    public bool HasAnyDisplayableWarning => HasDisplayableWarning || Config.HasDisplayableWarning;

    public bool HasDisplayableError => p_printedLastReloadError;
    public bool HasDisplayableWarning => false;

    public readonly string? ShellDirectory;

    public List<CommandInfo> LoadedCommands;
    public List<ModuleInfo> LoadedModules;

    public Game? ActiveGame;
    public Mod? ActiveMod;
    public List<HistoryItem> History;
    public string WorkingDirectory;

    private bool p_lastCancel;
    private bool p_printedCancel;

    private bool p_printedLastReloadError;

    private BackgroundWorker? p_activeCommand;

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
        else Config.LoadConfig(ShellDirectory);

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

        p_lastCancel = false;
        p_activeCommand = null;
        Console.CancelKeyPress += HandleCancel;

        ActiveGame = null;

        ReloadDirectoryInfo();
    }

    public bool LoadModule(Type moduleType)
    {
        if (LoadedModules.Any(x => x.Type.FullName == moduleType.FullName)) return false;

        ModuleInfo? module = ModuleInfo.FromType(moduleType);
        if (module is null) return false;

        LoadedModules.Add(module);
        LoadedCommands.AddRange(module.Commands);

        return true;
    }
    public bool LoadModule<T>() => LoadModule(typeof(T));
    public int LoadModules(Assembly moduleAssembly)
    {
        int loaded = 0;
        foreach (Type moduleType in moduleAssembly.GetTypes()) if (LoadModule(moduleType)) loaded++;
        return loaded;
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
        Write("\n", newLine: false);
        if (HasAnyDisplayableError) Write($"(Error) ", ConsoleColor.DarkRed, false);
        else if (HasAnyDisplayableWarning) Write($"(Warning) ", ConsoleColor.DarkYellow, false);

        if (ActiveMod is not null) Write($"{ActiveMod} ", ConsoleColor.Magenta, false);
        
        if (ActiveMod is not null && Config.LoadedConfig.UseLocalModDirectories)
        {
            string directory = Path.GetRelativePath(ActiveMod.RootDirectory, WorkingDirectory);
            if (directory == ".") directory = string.Empty;
            Write($"~\\{directory}", ConsoleColor.DarkGreen, false);
        }
        else Write($"{WorkingDirectory}", ConsoleColor.DarkGreen, false);

        if (ActiveGame is not null) Write($" ({ActiveGame})", ConsoleColor.Blue, false);
        Write(null);

        Write($" {Name}", ConsoleColor.DarkCyan, false);
        Write(" > ", ConsoleColor.White, false);

        bool printed = false;

        if (p_lastCancel && !p_printedCancel)
        {
            // Print the warning. A little bit of mess because execution must
            // continue without funny printing errors but it's alright I guess.

            int originalLeft = Console.CursorLeft;

            Console.CursorTop -= 3;
            Write("Press ^C again to exit the shell.", ConsoleColor.Red);
            PlayWarningSound();

            p_printedCancel = true;
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
            p_lastCancel = false;
            p_printedCancel = false;
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
                    try
                    {
                        command.Invoke(args);
                    }
#if RELEASE
                    catch (TargetInvocationException ex)
                    {
                        Write($"[ERROR] {ex.InnerException!.Message}", ConsoleColor.Red);
                        if (LoadingBar.Enabled) LoadingBar.End();
                    }
#endif
                    catch (Exception ex)
                    {
#if RELEASE
                        Write($"[ERROR] {ex.Message}", ConsoleColor.Red);
                        if (LoadingBar.Enabled) LoadingBar.End();
#else
                        Write($"[ERROR] {ex}", ConsoleColor.Red);
                        if (LoadingBar.Enabled) LoadingBar.End();
#endif
                    }
                }

                p_activeCommand = new();
                p_activeCommand.DoWork += runCommand;
                p_activeCommand.RunWorkerAsync();

                p_activeCommand.WorkerSupportsCancellation = command.CanBeCancelled;

                while (p_activeCommand is not null && p_activeCommand.IsBusy) Thread.Yield();

                if (p_activeCommand is not null)
                {
                    p_activeCommand.Dispose();
                    p_activeCommand = null;
                }

                if (ShellDirectory is null) Write("[WARNING] Could not save config to shell location. Any changes will be ignored.");
                else Config.SaveConfig(ShellDirectory);

                ReloadDirectoryInfo();
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
        try
        {
            ActiveMod = Mod.ReadDirectory(WorkingDirectory);
            ActiveGame = ActiveMod?.BaseGame;

            // Update title.
            string title = "SrcMod";
            if (ActiveMod is not null) title += $" - {ActiveMod.Name}";
            Console.Title = title;

            p_printedLastReloadError = false;
        }
        catch (Exception ex)
        {
            if (!p_printedLastReloadError)
            {
#if RELEASE
                Write("[ERROR] Error reloading directory information. Some data may not update.",
                    ConsoleColor.Red);
#else
                Write(ex, ConsoleColor.Red);
#endif
            }

            p_printedLastReloadError = true;
            Console.Title = "SrcMod (Error)";
        }
    }

    private void HandleCancel(object? sender, ConsoleCancelEventArgs args)
    {
        if (p_activeCommand is not null && p_activeCommand.IsBusy)
        {
            if (p_activeCommand.WorkerSupportsCancellation)
            {
                // Kill the active command.
                p_activeCommand.CancelAsync();
                p_activeCommand.Dispose();
                p_activeCommand = null;
            }
            else
            {
                // Command doesn't support cancellation.
                // Warn the user.
                PlayErrorSound();
            }

            p_lastCancel = false;
            p_printedCancel = false;
            args.Cancel = true;
            return;
        }

        // Due to some funny multithreading issues, we want to make the warning label
        // single-threaded on the shell.
        if (!p_lastCancel)
        {
            // Enable the warning. The "ReadLine" method will do the rest.
            p_lastCancel = true;
            args.Cancel = true; // "Cancel" referring to the cancellation of the cancel operation.
            return;
        }

        // Actually kill the shell. We do still have to worry about some multithreaded
        // nonsense, but a bearable amount of it.
        Console.ResetColor();
        Environment.Exit(0);
    }
}
