namespace SrcMod.Shell;

public static class Tools
{
    public static JsonSerializer Serializer { get; private set; }

    static Tools()
    {
        Serializer = JsonSerializer.Create(new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    public static void DisplayWithPages(IEnumerable<string> lines, ConsoleColor? color = null)
    {
        int written = 0;
        bool multiPage = false, hasQuit = false;
        foreach (string line in lines)
        {
            if (written == Console.BufferHeight - 2)
            {
                multiPage = true;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.ForegroundColor = ConsoleColor.Black;

                Console.Write(" -- More -- ");
                ConsoleKey key = Console.ReadKey(true).Key;
                Console.ResetColor();

                Console.CursorLeft = 0;

                if (key == ConsoleKey.Q)
                {
                    hasQuit = true;
                    break;
                }
                if (key == ConsoleKey.Spacebar) written = 0;
                else written--;
            }
            Write(line, color);
            written++;
        }

        if (multiPage)
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.Write(" -- End -- ");
            while (!hasQuit && Console.ReadKey(true).Key != ConsoleKey.Q) ;

            Console.ResetColor();

            Console.CursorLeft = 0;
            Console.Write("            ");
        }
    }

    public static IEnumerable<string> GetAllFiles(string directory, bool local = false) =>
        GetAllFiles(directory, local, directory);
    private static IEnumerable<string> GetAllFiles(string directory, bool local, string initialPath)
    {
        List<string> allFiles = new();
        foreach (string f in Directory.GetFiles(directory))
        {
            string path = Path.GetFullPath(f);
            if (local) path = Path.GetRelativePath(initialPath, path);
            allFiles.Add(path);
        }
        foreach (string dir in Directory.GetDirectories(directory))
            allFiles.AddRange(GetAllFiles(dir, local, initialPath));
        return allFiles;
    }

    public static void Write(object? message, ConsoleColor? col = null, bool newLine = true)
    {
        ConsoleColor prevCol = Console.ForegroundColor;
        if (col is not null) Console.ForegroundColor = col.Value;

        if (newLine) Console.WriteLine(message);
        else Console.Write(message);

        Console.ForegroundColor = prevCol;

        if (newLine && LoadingBar.Enabled && Console.CursorTop >= Console.BufferHeight - 1)
        {
            LoadingBar.position--;
            LoadingBar.Set(LoadingBar.value, LoadingBar.color);
        }
    }

    public static bool ValidateUnsafe()
    {
        switch (Config.LoadedConfig.RunUnsafeCommands)
        {
            case AskMode.Always:
                Write("[INFO] The shell has been configured to always run unsafe commands. " +
                      "This can be changed in the config.", ConsoleColor.DarkGray);
                return true;

            case AskMode.Never:
                Write("[ERROR] The shell has been configured to never run unsafe commands. " +
                      "This can be changed in the config.", ConsoleColor.Red);
                return false;

            case AskMode.Ask or _:
                Write("You are about to execute an unsafe command.\nProceed? > ", ConsoleColor.DarkYellow, false);
                Int2 start = (Console.CursorLeft, Console.CursorTop);
                Write("\nTip: You can disable this dialog in the config.", ConsoleColor.DarkGray);
                int finish = Console.CursorTop;

                Console.SetCursorPosition(start.x, start.y);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.CursorVisible = true;
                string result = Console.ReadLine()!.Trim().ToLower();
                Console.CursorVisible = false;
                Console.ResetColor();

                Console.SetCursorPosition(0, finish);

                return result == "y" || result == "yes" || result == "t" ||
                       result == "true" || result == "p" || result == "proceed";
        }
    }
}
