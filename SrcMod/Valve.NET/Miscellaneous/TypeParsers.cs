namespace Valve.Miscellaneous;

public static class TypeParsers
{
    public static bool CanParse(object? obj) => obj is not null && obj is bool or sbyte or byte or short or ushort
        or int or uint or long or ulong or Int128 or UInt128 or nint or nuint or Half or float or double or decimal
        or char or DateOnly or DateTime or DateTimeOffset or Guid or TimeOnly or TimeSpan;
    public static object ParseAll(string msg)
    {
        if (TryParseBool(msg, out bool resBool)) return resBool;
        else if (TryParse(msg, out sbyte int8)) return int8;
        else if (TryParse(msg, out byte uInt8)) return uInt8;
        else if (TryParse(msg, out short int16)) return int16;
        else if (TryParse(msg, out ushort uInt16)) return uInt16;
        else if (TryParse(msg, out int int32)) return int32;
        else if (TryParse(msg, out uint uInt32)) return uInt32;
        else if (TryParse(msg, out long int64)) return int64;
        else if (TryParse(msg, out ulong uInt64)) return uInt64;
        else if (TryParse(msg, out Int128 int128)) return int128;
        else if (TryParse(msg, out UInt128 uInt128)) return uInt128;
        else if (TryParse(msg, out nint intPtr)) return intPtr;
        else if (TryParse(msg, out nuint uIntPtr)) return uIntPtr;
        else if (TryParse(msg, out Half float16)) return float16;
        else if (TryParse(msg, out float float32)) return float32;
        else if (TryParse(msg, out double float64)) return float64;
        else if (TryParse(msg, out decimal float128)) return float128;
        else if (TryParse(msg, out char resChar)) return resChar;
        else if (TryParse(msg, out DateOnly dateOnly)) return dateOnly;
        else if (TryParse(msg, out DateTime dateTime)) return dateTime;
        else if (TryParse(msg, out DateTimeOffset dateTimeOffset)) return dateTimeOffset;
        else if (TryParse(msg, out Guid guid)) return guid;
        else if (TryParse(msg, out TimeOnly timeOnly)) return timeOnly;
        else if (TryParse(msg, out TimeSpan timeSpan)) return timeSpan;
        else return msg;
    }

    public static bool TryParseBool(string msg, out bool result)
    {
        string trimmed = msg.Trim().ToLower();

        string[] trues = new string[]
        {
            "t",
            "true",
            "1",
            "y",
            "yes"
        },
            falses = new string[]
        {
            "f",
            "false",
            "0",
            "n",
            "no"
        };

        foreach (string t in trues) if (trimmed == t)
            {
                result = true;
                return true;
            }
        foreach (string f in falses) if (trimmed == f)
            {
                result = false;
                return true;
            }

        result = false;
        return false;
    }
    public static bool TryParse<T>(string msg, out T? result) where T : IParsable<T>
        => T.TryParse(msg, null, out result);
}
