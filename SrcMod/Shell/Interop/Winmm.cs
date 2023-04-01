namespace SrcMod.Shell.Interop;

internal static partial class Winmm
{
    [LibraryImport("winmm.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PlaySound([MarshalAs(UnmanagedType.LPStr)] string pszSound, nint hMod, uint fdwSound);

    public enum PlaySoundFlags : uint
    {
        Sync = 0x00000000,
        Async = 0x00000001,
        NoDefault = 0x00000002,
        Memory = 0x00000004,
        Loop = 0x00000008,
        NoStop = 0x00000010,
        Purge = 0x00000040,
        Application = 0x00000080,
        NoWait = 0x00002000,
        Alias = 0x00010000,
        FileName = 0x00020000,
        Resource = 0x00040000,
        AliasId = 0x00100000,
    }
}
