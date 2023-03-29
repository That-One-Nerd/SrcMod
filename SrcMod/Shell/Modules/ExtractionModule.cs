namespace SrcMod.Shell.Modules;

[Module("extract")]
public static class ExtractionModule
{
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
