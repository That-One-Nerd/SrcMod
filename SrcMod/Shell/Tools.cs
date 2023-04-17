using System.Numerics;

namespace SrcMod.Shell;

public static class Tools
{
    public static JsonSerializer Serializer { get; private set; }

    private static int loadingPosition = -1;
    private static int lastLoadingBufferSize = 0;
    private static int lastLoadingValue = -1;
    private static float loadingBarValue = 0;
    private static ConsoleColor loadingBarColor = Console.ForegroundColor;

    public static bool LoadingBarEnabled { get; private set; }

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

        loadingBarValue = value;
        loadingBarColor = color ?? Console.ForegroundColor;

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
        while (oldPos.y >= Console.BufferHeight)
        {
            Console.WriteLine();
            oldPos.y--;
            loadingPosition--;
        }
        Console.SetCursorPosition(oldPos.x, oldPos.y);
    }
    public static void LoadingBarStart(float value = 0, int? position = null, ConsoleColor? color = null)
    {
        if (loadingPosition != -1) throw new("The loading bar has already been enabled.");
        loadingPosition = position ?? Console.CursorTop;
        LoadingBarEnabled = true;
        LoadingBarSet(value, color);
    }

    public static void Write(object? message, ConsoleColor? col = null, bool newLine = true)
    {
        ConsoleColor prevCol = Console.ForegroundColor;
        if (col is not null) Console.ForegroundColor = col.Value;

        if (newLine) Console.WriteLine(message);
        else Console.Write(message);

        Console.ForegroundColor = prevCol;

        if (newLine && LoadingBarEnabled && Console.CursorTop >= Console.BufferHeight - 1)
        {
            loadingPosition--;
            LoadingBarSet(loadingBarValue, loadingBarColor);
        }
    }

    public static bool ValidateUnsafe()
    {
        Write("You are about to execute an unsafe command.\nProceed? > ", ConsoleColor.DarkYellow, false);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.CursorVisible = true;
        string result = Console.ReadLine()!.Trim().ToLower();
        Console.CursorVisible = false;
        Console.ResetColor();

        return result == "y" || result == "yes" || result == "t" ||
               result == "true" || result == "p" || result == "proceed";
    }
}
