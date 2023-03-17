namespace SrcMod.Shell;

public static class Tools
{
    private static int loadingPosition = -1;
    private static int lastLoadingBufferSize = 0;
    private static int lastLoadingValue = -1;

    public static bool LoadingBarEnabled { get; private set; }

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

    public static void LoadingBarEnd(bool clear = true)
    {
        if (loadingPosition == -1) throw new("No loading bar is active.");

        if (clear)
        {
            Int2 oldPos = (Console.CursorLeft, Console.CursorTop);

            Console.CursorLeft = 0;
            Console.CursorTop = loadingPosition;
            Console.Write(new string(' ', Console.BufferWidth));
            Console.CursorLeft = 0;

            Console.SetCursorPosition(oldPos.x, oldPos.y);
        }
        loadingPosition = -1;
        LoadingBarEnabled = false;
    }
    public static void LoadingBarSet(float value, ConsoleColor? color = null)
    {
        const string left = " --- [",
                     right = "] --- ";
        int barSize = Console.BufferWidth - left.Length - right.Length,
            filled = (int)(barSize * value);

        if (filled == lastLoadingValue) return;
        lastLoadingValue = filled;

        Int2 oldPos = (Console.CursorLeft, Console.CursorTop);

        // Erase last bar.
        Console.SetCursorPosition(0, loadingPosition);
        Console.Write(new string(' ', lastLoadingBufferSize));
        Console.CursorLeft = 0;

        // Add new bar.
        lastLoadingBufferSize = Console.BufferWidth;

        Write(left, newLine: false);
        ConsoleColor oldFore = Console.ForegroundColor;

        if (color is not null) Console.ForegroundColor = color.Value;
        Write(new string('=', filled), newLine: false);
        if (color is not null) Console.ForegroundColor = oldFore;
        Write(new string(' ', barSize - filled), newLine: false);
        Write(right, newLine: false);

        if (oldPos.y == Console.CursorTop) oldPos.y++;
        Console.SetCursorPosition(oldPos.x, oldPos.y);
    }
    public static void LoadingBarStart(float value = 0, int? position = null, ConsoleColor? color = null)
    {
        if (loadingPosition != -1) throw new("The loading bar has already been enabled.");
        loadingPosition = position ?? Console.CursorTop;
        LoadingBarSet(value, color);
        LoadingBarEnabled = true;
    }

    public static void Write(object? message, ConsoleColor? col = null, bool newLine = true)
    {
        ConsoleColor prevCol = Console.ForegroundColor;
        if (col is not null) Console.ForegroundColor = col.Value;

        if (newLine) Console.WriteLine(message);
        else Console.Write(message);

        Console.ForegroundColor = prevCol;
    }
}
