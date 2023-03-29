namespace SrcMod.Shell.Modules;

[Module("extract")]
public static class ExtractionModule
{
    [Command("gz")]
    [Command("gzip")]
    public static void ExtractGZip(string source, string? destination = null)
    {
        if (!File.Exists(source)) throw new($"No file exists at \"{source}\".");

        if (destination is null)
        {
            string full = Path.GetFullPath(source);
            string folder = Program.Shell!.WorkingDirectory;
            string name = Path.GetFileNameWithoutExtension(full);

            destination = $"{folder}\\{name}";
        }

        string absSource = Path.GetFullPath(source),
               localDest = Path.GetRelativePath(Program.Shell!.WorkingDirectory, destination);

        if (File.Exists(destination)) throw new($"File already exists at \"{destination}\".");
        string message = $"Extracting file at \"{source}\" into \"{localDest}\"...";
        Write(message);

        FileStream writer = new(destination, FileMode.CreateNew),
                   reader = new(absSource, FileMode.Open);
        GZipStream gzip = new(reader, CompressionMode.Decompress);

        LoadingBarStart();

        int bufferSize = Mathf.Clamp((int)reader.Length / Console.BufferWidth, 1024 * 1024, 128 * 1024 * 1024);
        byte[] buffer = new byte[bufferSize];
        int i = 0;
        int size;
        while ((size = gzip.Read(buffer, i, bufferSize)) > 0)
        {
            writer.Write(buffer, 0, size);
            writer.Flush();

            LoadingBarSet((float)i / reader.Length, ConsoleColor.DarkGreen);
        }

        LoadingBarEnd();

        gzip.Close();
        reader.Close();
        writer.Close();

        Console.CursorLeft = 0;
        Console.CursorTop -= (message.Length / Console.BufferWidth) + 2;
        Write(new string(' ', message.Length), newLine: false);
    }

    [Command("zip")]
    public static void ExtractZip(string source, string? destination = null)
    {
        if (!File.Exists(source)) throw new($"No file exists at \"{source}\".");

        if (destination is null)
        {
            string full = Path.GetFullPath(source);
            string folder = Program.Shell!.WorkingDirectory;
            string name = Path.GetFileNameWithoutExtension(full);

            destination = $"{folder}\\{name}";
        }

        if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);

        FileStream reader = new(source, FileMode.Open);
        ZipArchive zip = new(reader, ZipArchiveMode.Read);

        if (!string.IsNullOrWhiteSpace(zip.Comment)) Write(zip.Comment);

        zip.ExtractToDirectory(destination, true);

        zip.Dispose();
        reader.Dispose();
    }
}
