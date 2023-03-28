using SrcMod.Shell.Interop;

namespace SrcMod.Shell.Modules;

[Module("clipboard")]
public static class ClipboardModule
{
    [Command("clear")]
    public static void ClearClipboard()
    {
        if (!ValidateUnsafe()) return;

        User32.OpenClipboard(0);
        User32.EmptyClipboard();
        User32.CloseClipboard();
    }

    [Command("copy")]
    public static void CopyClipboard(string text)
    {
        const uint format = 1;

        if (!ValidateUnsafe()) return;

        if (!text.EndsWith("\0")) text += "\0";
        byte[] data = Encoding.Default.GetBytes(text);

        nint hGlobal = Marshal.AllocHGlobal(data.Length + 1);
        Marshal.Copy(data, 0, hGlobal, data.Length);

        User32.OpenClipboard(0);
        User32.EmptyClipboard();

        User32.SetClipboardData(format, hGlobal);

        User32.CloseClipboard();
    }

    [Command("view")]
    public static void ViewClipboard()
    {
        // TODO (maybe): Make this support other formats?
        // I spent way too long trying to make that a reality
        // but the whole "clipboard format" thing is a nightmare.

        if (!ValidateUnsafe()) return;

        User32.OpenClipboard(0);

        nint hClipboard;
        uint format;
        if (User32.IsClipboardFormatAvailable(format = (uint)ClipboardFormat.DspText) ||
            User32.IsClipboardFormatAvailable(format = (uint)ClipboardFormat.OemText) ||
            User32.IsClipboardFormatAvailable(format = (uint)ClipboardFormat.Text) ||
            User32.IsClipboardFormatAvailable(format = (uint)ClipboardFormat.UnicodeText))
                hClipboard = User32.GetClipboardData(format);
        else throw new("Clipboard doesn't contain text data.");

        nuint length = Kernel32.GlobalSize(hClipboard);
        byte[] data = new byte[length];

        Marshal.Copy(hClipboard, data, 0, (int)length);

        User32.CloseClipboard();

        string msg = (ClipboardFormat)format switch
        {
            ClipboardFormat.DspText or ClipboardFormat.OemText or ClipboardFormat.Text =>
                Encoding.UTF8.GetString(data),
            ClipboardFormat.UnicodeText => Encoding.Unicode.GetString(data),
            _ => throw new("Unknown text format.")
        };

        Write(msg);
    }

    public enum ClipboardFormat
    {
        Biff5 = 49988,
        Biff8 = 49986,
        Biff12 = 50009,
        Bitmap = 2,
        Csv = 49989,
        DataObject = 49161,
        Dib = 8,
        Dif = 5,
        DspText = 129,
        EmbedSource = 49163,
        EnhancedMetafile = 14,
        HandleDrop = 15,
        HtmlFormat = 49381,
        Hyperlink = 50006,
        Link = 49985,
        LinkSource = 49165,
        LinkSourceDescriptor = 49167,
        Locale = 16,
        Max = 17,
        MetafilePicture = 3,
        Native = 49156,
        ObjectDescriptor = 49166,
        ObjectLink = 49154,
        OemText = 7,
        OlePrivateData = 49171,
        OwnerLink = 49155,
        Palette = 9,
        RichTextFormat = 49308,
        SYLK = 4,
        Text = 1,
        UnicodeText = 13,
        XmlSpreadSheet = 50007
    }
}
