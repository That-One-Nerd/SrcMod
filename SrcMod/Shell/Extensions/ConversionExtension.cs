namespace SrcMod.Shell.Extensions;

public static class ConversionExtension
{
    public static T Cast<T>(this object obj) => (T)Cast(obj, typeof(T));
    public static object Cast(this object obj, Type newType) => Convert.ChangeType(obj, newType);

    public static object CastArray(this object[] obj, Type newElementType)
    {
        Array result = Array.CreateInstance(newElementType, obj.Length);
        for (int i = 0; i < obj.Length; i++) result.SetValue(obj[i].Cast(newElementType), i);
        return result;
    }
    public static T[] CastArray<T>(this object[] obj)
    {
        Array result = Array.CreateInstance(typeof(T), obj.Length);
        for (int i = 0; i < obj.Length; i++) result.SetValue(obj[i].Cast<T>(), i);
        return (T[])result;
    }
}
