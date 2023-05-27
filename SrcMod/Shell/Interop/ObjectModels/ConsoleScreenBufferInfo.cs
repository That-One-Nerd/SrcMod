namespace SrcMod.Shell.Interop.ObjectModels;

[StructLayout(LayoutKind.Sequential)]
internal struct ConsoleScreenBufferInfo
{
    [MarshalAs(UnmanagedType.LPStruct)]
    public Coord dwSize;
    [MarshalAs(UnmanagedType.LPStruct)]
    public Coord dwCursorPosition;
    public int wAttributes;
    [MarshalAs(UnmanagedType.LPStruct)]
    public SmallRect srWindow;
    [MarshalAs(UnmanagedType.LPStruct)]
    public Coord dwMaximumWindowSize;
}
