namespace SrcMod.Shell.Modules.ObjectModels;

public static class TypeParsers
{
    public static bool CanParse(object? obj) => obj is not null && obj is sbyte or byte or short or ushort or int
        or uint or long or ulong or Int128 or UInt128 or nint or nuint or Half or float or double or decimal
        or char or DateOnly or DateTime or DateTimeOffset or Guid or TimeOnly or TimeSpan;
    public static object ParseAll(string msg)
    {
        if (TryParse(msg, out sbyte int8)) return int8;
        if (TryParse(msg, out byte uInt8)) return uInt8;
        if (TryParse(msg, out short int16)) return int16;
        if (TryParse(msg, out ushort uInt16)) return uInt16;
        if (TryParse(msg, out int int32)) return int32;
        if (TryParse(msg, out uint uInt32)) return uInt32;
        if (TryParse(msg, out long int64)) return int64;
        if (TryParse(msg, out ulong uInt64)) return uInt64;
        if (TryParse(msg, out Int128 int128)) return int128;
        if (TryParse(msg, out UInt128 uInt128)) return uInt128;
        if (TryParse(msg, out nint intPtr)) return intPtr;
        if (TryParse(msg, out nuint uIntPtr)) return uIntPtr;
        if (TryParse(msg, out Half float16)) return float16;
        if (TryParse(msg, out float float32)) return float32;
        if (TryParse(msg, out double float64)) return float64;
        if (TryParse(msg, out decimal float128)) return float128;
        if (TryParse(msg, out char resChar)) return resChar;
        if (TryParse(msg, out DateOnly dateOnly)) return dateOnly;
        if (TryParse(msg, out DateTime dateTime)) return dateTime;
        if (TryParse(msg, out DateTimeOffset dateTimeOffset)) return dateTimeOffset;
        if (TryParse(msg, out Guid guid)) return guid;
        if (TryParse(msg, out TimeOnly timeOnly)) return timeOnly;
        if (TryParse(msg, out TimeSpan timeSpan)) return timeSpan;

        return msg;
    }

    public static bool TryParse<T>(string msg, out T? result) where T : IParsable<T>
        => T.TryParse(msg, null, out result);
}
