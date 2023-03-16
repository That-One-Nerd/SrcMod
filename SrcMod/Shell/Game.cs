namespace SrcMod.Shell;

public class Game
{
    public static readonly Game Portal2 = new()
    {
        Name = "Portal 2",
        NameId = "portal2",
        SteamId = 620
    };

    public required string Name { get; init; }
    public required string NameId { get; init; }
    public required int SteamId { get; init; }

    private Game() { }

    public override string ToString() => Name;
}
