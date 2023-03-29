namespace SrcMod.Shell.Modules;

[Module("compress")]
public static class CompressionModule
{
    [Command("zip")]
    public static void CompressZip(string source, string? destination = null,
        CompressionLevel level = CompressionLevel.Optimal, string comment = "")
    {
        destination ??= Path.Combine(Path.GetDirectoryName(Path.GetFullPath(source))!,
                                     $"{Path.GetFileNameWithoutExtension(source)}.zip");

        string absSource = Path.GetFullPath(source),
               localDest = Path.GetRelativePath(Program.Shell!.WorkingDirectory, destination);

        if (File.Exists(source))
        {
            if (File.Exists(destination)) throw new($"File already exists at \"{localDest}\"");
            string message = $"Compressing file at \"{source}\" into \"{localDest}\"...";
            Write(message);

            Stream writer = new FileStream(destination, FileMode.CreateNew);
            ZipArchive archive = new(writer, ZipArchiveMode.Create)
            {
                Comment = comment
            };

            archive.CreateEntryFromFile(absSource, Path.GetFileName(absSource), level);

            archive.Dispose();
            writer.Dispose();

            Console.CursorLeft = 0;
            Console.CursorTop -= (message.Length / Console.BufferWidth) + 1;
            Write(new string(' ', message.Length), newLine: false);
        }
        else if (Directory.Exists(source))
        {
            if (File.Exists(destination)) throw new($"File already exists at \"{localDest}\"");

            Write($"Compressing folder at \"{source}\" into \"{localDest}\"...");

            Stream writer = new FileStream(destination, FileMode.CreateNew);
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

        DateTime stamp = DateTime.Now;

        Program.Shell!.AddHistory(new()
        {
            action = delegate
            {
                if (!File.Exists(destination))
                {
                    Write("Looks like the job is already completed Boss.", ConsoleColor.DarkYellow);
                    return;
                }

                FileInfo info = new(destination);
                if ((info.LastWriteTime - stamp).TotalMilliseconds >= 10)
                    throw new("The archive has been modified and probably shouldn't be undone.");

                File.Delete(destination);
            },
            name = $"Compressed a file or folder into a zip archive located at \"{destination}\""
        });
    }
}
