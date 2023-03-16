namespace SrcMod.Shell;

public static class Program
{
    public static Shell? Shell { get; private set; }

    public static void Main(string[] args)
    {
        Console.Clear();

        // Check for arguments and send a warning if they are found.
        // In the future, I may use these arguments.
        if (args.Length != 0) Write("[WARNING] You have supplied this shell " +
            "with arguments. They will be ignored.", ConsoleColor.DarkYellow);

        Shell = new();

        while (true)
        {
            string cmd = Shell.ReadLine();
            Shell.InvokeCommand(cmd);
            Shell.ReloadDirectoryInfo();
        }
    }
}
