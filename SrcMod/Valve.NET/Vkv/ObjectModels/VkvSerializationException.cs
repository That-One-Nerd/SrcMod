namespace Valve.Vkv.ObjectModels;

public class VkvSerializationException : Exception
{
    public VkvSerializationException() : base() { }
    public VkvSerializationException(string message) : base(message) { }
    public VkvSerializationException(string message, Exception inner) : base(message, inner) { }
}
