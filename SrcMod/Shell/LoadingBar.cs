namespace SrcMod.Shell;

internal static class LoadingBar
{
    public static int position = -1;
    public static int bufferSize = 0;
    public static int lastValue = -1;
    public static float value = 0;
    public static ConsoleColor color = Console.ForegroundColor;

    public static bool Enabled { get; private set; }

    public static void End(bool clear = true)
    {
        if (position == -1) throw new("No loading bar is active.");

        if (clear)
        {
            Int2 oldPos = (Console.CursorLeft, Console.CursorTop);

            Console.CursorLeft = 0;
            Console.CursorTop = position;
            Console.Write(new string(' ', Console.BufferWidth));
            Console.CursorLeft = 0;

            Console.SetCursorPosition(oldPos.x, oldPos.y);
        }
        position = -1;
        Enabled = false;
    }
    public static void Set(float value, ConsoleColor? color = null)
    {
        const string left = " --- [",
                     right = "] --- ";
        int barSize = Console.BufferWidth - left.Length - right.Length,
            filled = (int)(barSize * value);

        if (filled == lastValue) return;
        lastValue = filled;

        Int2 oldPos = (Console.CursorLeft, Console.CursorTop);

        LoadingBar.value = value;
        LoadingBar.color = color ?? Console.ForegroundColor;

        // Erase last bar.
        Console.SetCursorPosition(0, position);
        Console.Write(new string(' ', bufferSize));
        Console.CursorLeft = 0;

        // Add new bar.
        bufferSize = Console.BufferWidth;

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
            position--;
        }
        Console.SetCursorPosition(oldPos.x, oldPos.y);
    }
    public static void Start(float value = 0, int? position = null, ConsoleColor? color = null)
    {
        if (LoadingBar.position != -1) throw new("The loading bar has already been enabled.");
        LoadingBar.position = position ?? Console.CursorTop;
        Enabled = true;
        Set(value, color);
    }
}
