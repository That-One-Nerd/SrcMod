﻿using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace SrcMod.Shell.Modules;

[Module("base", false)]
public static class BaseModule
{
    [Command("cd")]
    public static void ChangeDirectory(string newLocalPath)
    {
        string curDir = Program.Shell!.WorkingDirectory,
               newDir = Path.GetFullPath(Path.Combine(curDir, newLocalPath));
        Program.Shell!.UpdateWorkingDirectory(newDir);
        Environment.CurrentDirectory = newDir;
    }

    [Command("clear")]
    public static void ClearConsole()
    {
        Console.Clear();
        Console.Write("\x1b[3J");
    }

    [Command("compress")]
    public static void CompressFile(CompressedFileType type, string source, string? destination = null,
        CompressionLevel level = CompressionLevel.Optimal)
    {
        destination ??= Path.Combine(Path.GetDirectoryName(source)!,
                                     $"{Path.GetFileNameWithoutExtension(source)}.{type.ToString().ToLower()}");

        string absSource = Path.GetFullPath(source),
               absDest = Path.GetFullPath(destination);

        switch (type)
        {
            case CompressedFileType.Zip:
                if (File.Exists(source))
                {
                    if (File.Exists(destination)) throw new($"File already exists at \"{destination}\"");
                    string message = $"Compressing file at \"{source}\" into \"{destination}\"...";
                    Write(message);

                    Stream writer = new FileStream(absDest, FileMode.CreateNew);
                    ZipArchive archive = new(writer, ZipArchiveMode.Create);

                    archive.CreateEntryFromFile(absSource, Path.GetFileName(absSource), level);

                    archive.Dispose();
                    writer.Dispose();

                    Console.CursorLeft = 0;
                    Console.CursorTop -= (message.Length / Console.BufferWidth) + 1;
                    Write(new string(' ', message.Length), newLine: false);
                }
                else if (Directory.Exists(source))
                {
                    if (File.Exists(destination)) throw new($"File already exists at \"{destination}\"");

                    Write($"Compressing folder at \"{source}\" into \"{destination}\"...");

                    Stream writer = new FileStream(absDest, FileMode.CreateNew);
                    ZipArchive archive = new(writer, ZipArchiveMode.Create);

                    List<string> files = new(GetAllFiles(absSource)),
                                 relative = new();
                    foreach (string f in files) relative.Add(Path.GetRelativePath(absSource, f));

                    LoadingBarStart();
                    for (int i = 0; i < files.Count; i++)
                    {
                        archive.CreateEntryFromFile(files[i], relative[i], level);
                        LoadingBarSet((i + 1) / (float)files.Count, ConsoleColor.DarkGreen);
                        Console.CursorLeft = 0;
                        string message = $"{relative[i]}";
                        int remainder = Console.BufferWidth - message.Length;
                        if (remainder >= 0) message += new string(' ', remainder);
                        else message = $"...{message[(3 - remainder)..]}";

                        Write(message, newLine: false);
                    }

                    archive.Dispose();
                    writer.Dispose();

                    LoadingBarEnd();

                    Console.CursorLeft = 0;
                    Write(new string(' ', Console.BufferWidth), newLine: false);
                    Console.SetCursorPosition(0, Console.CursorTop - 2);
                    Write(new string(' ', Console.BufferWidth), newLine: false);
                }
                else throw new("No file or directory located at \"source\"");
                break;

            default: throw new($"Unknown type: \"{type}\"");
        }

        DateTime stamp = DateTime.Now;

        Program.Shell!.AddHistory(new()
        {
            action = delegate
            {
                if (!File.Exists(absDest))
                {
                    Write("Looks like the job is already completed Boss.", ConsoleColor.DarkYellow);
                    return;
                }

                FileInfo info = new(absDest);
                if ((info.LastWriteTime - stamp).TotalMilliseconds >= 10)
                    throw new("The archive has been modified and probably shouldn't be undone.");

                File.Delete(absDest);
            },
            name = $"Compressed a file or folder into a {type} archive located at \"{destination}\""
        });
    }

    [Command("copy")]
    public static void CopyFile(string source, string destination)
    {
        string absSource = Path.GetFullPath(source),
               absDest = Path.GetFullPath(destination);

        if (File.Exists(source))
        {
            if (File.Exists(destination)) throw new($"File already exists at \"{destination}\"");

            string message = $"Copying file \"{source}\" to \"{destination}\"...";
            Write(message);

            File.Copy(source, destination);

            Console.CursorLeft = 0;
            Console.CursorTop -= (message.Length / Console.BufferWidth) + 1;
            Write(new string(' ', message.Length), newLine: false);
        }
        else if (Directory.Exists(source))
        {
            if (Directory.Exists(destination)) throw new($"Directory already exists at \"{destination}\"");
            string[] files = GetAllFiles(source, true).ToArray();

            Write($"Copying directory \"{source}\" to \"{destination}\"...");

            LoadingBarStart();
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i],
                       sourceFile = Path.Combine(source, file),
                       destFile = Path.Combine(destination, file);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(sourceFile, destFile);
                LoadingBarSet((i + 1) / (float)files.Length, ConsoleColor.DarkGreen);
                Console.CursorLeft = 0;
                string message = $"{sourceFile}";
                int remainder = Console.BufferWidth - message.Length;
                if (remainder >= 0) message += new string(' ', remainder);
                else message = $"...{message[(3 - remainder)..]}";

                Write(message, newLine: false);
            }

            LoadingBarEnd();

            Console.CursorLeft = 0;
            Write(new string(' ', Console.BufferWidth), newLine: false);
            Console.SetCursorPosition(0, Console.CursorTop - 2);
            Write(new string(' ', Console.BufferWidth), newLine: false);
        }
        else throw new($"No file or directory found at \"{source}\"");

        DateTime stamp = DateTime.Now;

        Program.Shell!.AddHistory(new()
        {
            action = delegate
            {
                if (!File.Exists(absDest))
                {
                    Write("Looks like the job is already completed Boss.", ConsoleColor.DarkYellow);
                    return;
                }

                FileInfo info = new(absDest);
                if ((info.LastWriteTime - stamp).TotalMilliseconds >= 10)
                    throw new("The copied file or folder has been modified and probably shouldn't be undone.");

                File.Delete(absDest);
            },
            name = $"Copied a file or folder from \"{absSource}\" to \"{absDest}\""
        });
    }

    [Command("cut")]
    public static void CutFile(string source, string destination) => MoveFile(source, destination);

    [Command("del")]
    public static void Delete(string path)
    {
        if (File.Exists(path))
        {
            string tempFile = Path.GetTempFileName();
            File.Delete(tempFile);
            File.Copy(path, tempFile);
            File.Delete(path);

            Program.Shell!.AddHistory(new()
            {
                action = delegate
                {
                    if (File.Exists(path)) throw new("Can't overwrite already existing file.");
                    File.Copy(tempFile, path);
                    File.Delete(tempFile);
                },
                name = $"Deleted file \"{Path.GetFileName(path)}\""
            });
        }
        else if (Directory.Exists(path))
        {
            string[] parts = path.Replace("/", "\\").Split('\\',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
            Directory.Delete(tempDir.FullName);
            Directory.Move(path, tempDir.FullName);

            Program.Shell!.AddHistory(new()
            {
                action = delegate
                {
                    if (Directory.Exists(path)) throw new("Can't overwrite already existing file.");
                    Directory.Move(tempDir.FullName, path);
                },
                name = $"Deleted directory \"{parts.Last()}\""
            });
        }
        else throw new($"No file or directory exists at \"{path}\"");
    }

    [Command("dir")]
    public static void ListFilesAndDirs(string path = ".")
    {
        string[] dirs = Directory.GetDirectories(path),
                 files = Directory.GetFiles(path);

        List<string> lines = new();

        int longestName = 0,
            longestSize = 0;
        foreach (string d in dirs) if (d.Length > longestName) longestName = d.Length;
        foreach (string f in files)
        {
            FileInfo info = new(f);
            if (f.Length > longestName) longestName = f.Trim().Length;

            int size = Mathf.Ceiling(MathF.Log10(info.Length));
            if (longestSize > size) longestSize = size;
        }

        string header = $"  Type       Name{new string(' ', longestName - 4)}Date Modified        File Size" +
                        $"{new string(' ', Mathf.Max(0, longestSize - 10) + 1)}";
        lines.Add($"{header}\n{new string('-', header.Length)}");

        foreach (string d in dirs)
        {
            DirectoryInfo info = new(d);
            lines.Add($" Directory  {info.Name.Trim()}{new string(' ', longestName - info.Name.Trim().Length)}" +
                  $"{info.LastWriteTime:MM/dd/yyyy HH:mm:ss}");
        }
        foreach (string f in files)
        {
            FileInfo info = new(f);
            lines.Add($" File       {info.Name.Trim()}{new string(' ', longestName - info.Name.Trim().Length)}" +
                  $"{info.LastWriteTime:MM/dd/yyyy HH:mm:ss}  {info.Length}");
        }

        DisplayWithPages(lines);
    }

    [Command("echo")]
    public static void Echo(string msg) => Write(msg);

    [Command("exit")]
    public static void ExitShell(int code = 0) => QuitShell(code);

    [Command("explorer")]
    public static void OpenExplorer(string path = ".") => Process.Start("explorer.exe", Path.GetFullPath(path));

    [Command("history")]
    public static void ShowHistory()
    {
        List<string> lines = new() { " Timestamp           Description"};
        int longestName = 0;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            HistoryItem hist = Program.Shell!.History[i];
            if (hist.name.Length > longestName) longestName = hist.name.Length;
            lines.Add(hist.ToString());
        }
        lines.Insert(1, new string('-', 22 + longestName));
        DisplayWithPages(lines);
    }

    [Command("move")]
    public static void MoveFile(string source, string destination)
    {
        string absSource = Path.GetFullPath(source),
               absDest = Path.GetFullPath(destination);

        if (File.Exists(source))
        {
            if (File.Exists(destination)) throw new($"File already exists at \"{destination}\"");

            string message = $"Moving file \"{source}\" to \"{destination}\"...";
            Write(message);

            File.Move(source, destination);

            Console.CursorLeft = 0;
            Console.CursorTop -= (message.Length / Console.BufferWidth) + 1;
            Write(new string(' ', message.Length), newLine: false);
        }
        else if (Directory.Exists(source))
        {
            if (Directory.Exists(destination)) throw new($"Directory already exists at \"{destination}\"");

            string message = $"Moving directory \"{source}\" to \"{destination}\"...";
            Write(message);

            Directory.Move(source, destination);

            Console.CursorLeft = 0;
            Console.CursorTop -= (message.Length / Console.BufferWidth) + 1;
            Write(new string(' ', message.Length), newLine: false);
        }
        else throw new($"No file or directory found at \"{source}\"");

        DateTime stamp = DateTime.Now;

        Program.Shell!.AddHistory(new()
        {
            action = delegate
            {
                if (!File.Exists(absDest))
                {
                    Write("Looks like the job is already completed Boss.", ConsoleColor.DarkYellow);
                    return;
                }

                FileInfo info = new(absDest);
                if ((info.LastWriteTime - stamp).TotalMilliseconds >= 10)
                    throw new("The moved file or folder has been modified and probably shouldn't be undone.");

                File.Delete(absDest);
            },
            name = $"Moved a file or folder from \"{absSource}\" to \"{absDest}\""
        });
    }

    [Command("permdel")]
    public static void ReallyDelete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        else if (Directory.Exists(path)) Directory.Delete(path);
        else throw new($"No file or directory exists at \"{path}\"");
    }

    [Command("print")]
    public static void Print(string file)
    {
        if (!File.Exists(file)) throw new($"No file exists at \"{file}\"");
        StreamReader reader = new(file);
        Write(reader.ReadToEnd());
    }

    [Command("sleep")]
    public static void WaitTime(int timeMs) => Thread.Sleep(timeMs);

    [Command("srcmod")]
    public static void EasterEgg()
    {
        // THIS IS A JOKE IF YOU CAN'T TELL

        // Get sourcemod dirs.
        const string path = "C:\\Program Files (x86)\\Steam\\steamapps\\sourcemods";
        string[] mods = Directory.GetDirectories(path);

        Write($"Resetting all {mods.Length} source mods to default value \"none\"...");
        string[] files = GetAllFiles(path).ToArray();

        Random rand = new();

        Thread.Sleep(rand.Next(500, 1000));

        LoadingBarStart();

        for (int i = 0; i < files.Length; i++)
        {
            FileInfo file = new(files[i]);
            Thread.Sleep((int)(rand.Next(50, 100) * (file.Length >> 20)));
            LoadingBarSet((i + 1) / (float)files.Length, ConsoleColor.Red);
            Console.CursorLeft = 0;
            string message = $"{files[i]}";
            int remainder = Console.BufferWidth - message.Length;
            if (remainder >= 0) message += new string(' ', remainder);
            else message = $"...{message[(3 - remainder)..]}";

            Write(message, newLine: false);
        }

        LoadingBarEnd();

        Console.CursorLeft = 0;
        Write(new string(' ', Console.BufferWidth), newLine: false);
        Console.SetCursorPosition(0, Console.CursorTop - 2);
        Write(new string(' ', Console.BufferWidth), newLine: false);

        Program.Shell!.AddHistory(new()
        {
            action = delegate
            {
                Write("You cannot undo this operation.", ConsoleColor.DarkYellow);
            },
            name = "Reset all source mods."
        });
    }

    [Command("quit")]
    public static void QuitShell(int code = 0)
    {
        Environment.Exit(code);
    }

    [Command("type")]
    public static void Type(string file) => Print(file);

    [Command("undo")]
    public static void UndoCommand(int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            if (Program.Shell!.History.Count < 1)
            {
                if (i == 0) throw new("No operations to undo.");
                else
                {
                    Write("No more operations to undo.", ConsoleColor.DarkYellow);
                    break;
                }
            }
            Program.Shell!.UndoItem();
        }
    }

    public enum CompressedFileType
    {
        Zip
    }
}
