﻿namespace SrcMod.Shell.Interop;

internal static partial class Kernel32
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint GetFinalPathNameByHandleA(nint hFile, [MarshalAs(UnmanagedType.LPTStr)] string lpszFilePath,
        uint cchFilePath, uint dwFlags);

    [LibraryImport("kernel32.dll")]
    public static partial nuint GlobalSize(nint hPtr);
}
