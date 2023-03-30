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

        gzip.CopyTo(writer);

        gzip.Close();
        reader.Close();
        writer.Close();

        Console.CursorLeft = 0;
        Console.CursorTop -= (message.Length / Console.BufferWidth) + 1;
        Write(new string(' ', message.Length), newLine: false);
    }

    [Command("tar")]
    [Command("tarball")]
    public static void ExtractTar(string source, string? destination = null)
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
        TarFile.ExtractToDirectory(reader, Path.GetFileName(destination), true);

        reader.Dispose();
    }

    [Command("targz")]
    [Command("tar.gz")]
    [Command("tar-gz")]
    public static void ExtractTarGz(string source, string? destination = null)
    {
        if (!File.Exists(source)) throw new($"No file exists at \"{source}\".");

        if (destination is null)
        {
            string full = Path.GetFullPath(source);
            string folder = Program.Shell!.WorkingDirectory;
            string name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(full));

            destination = $"{folder}\\{name}";
        }

        string absSource = Path.GetFullPath(source),
               temp = Path.Combine(Path.GetDirectoryName(absSource)!, Path.GetFileNameWithoutExtension(absSource));

        ExtractGZip(source, temp);
        ExtractTar(temp, destination);

        File.Delete(temp);
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
