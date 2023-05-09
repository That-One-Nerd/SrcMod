namespace SrcMod.Shell.Valve.ObjectModels;

public class VdfSerializationException : Exception
{
    public VdfSerializationException() : base() { }
    public VdfSerializationException(string message) : base(message) { }
    public VdfSerializationException(string message, Exception inner) : base(message, inner) { }
}
