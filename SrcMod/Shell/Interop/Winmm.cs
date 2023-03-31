namespace SrcMod.Shell.Interop;

internal static partial class Winmm
{
    [LibraryImport("winmm.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PlaySound([MarshalAs(UnmanagedType.LPStr)] string pszSound, nint hMod, uint fdwSound);

    public enum PlaySoundFlags : uint
    {
        SND_SYNC = 0x00000000,
        SND_ASYNC = 0x00000001,
        SND_NODEFAULT = 0x00000002,
        SND_MEMORY = 0x00000004,
        SND_LOOP = 0x00000008,
        SND_NOSTOP = 0x00000010,
        SND_PURGE = 0x00000040,
        SND_APPLICATION = 0x00000080,
        SND_NOWAIT = 0x00002000,
        SND_ALIAS = 0x00010000,
        SND_FILENAME = 0x00020000,
        SND_RESOURCE = 0x00040000,
        SND_ALIAS_ID = 0x00100000,
    }
}
