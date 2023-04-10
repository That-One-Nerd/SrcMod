namespace SrcMod.Shell.ObjectModels;

public record struct ConfigChanges
{
    [JsonIgnore]
    public bool HasChange => GameDirectories is not null || RunUnsafeCommands is not null;

    public string[]? GameDirectories;
    public AskMode? RunUnsafeCommands;
}
