namespace SrcMod.Shell;

public class Game : IEquatable<Game>
{
    public static readonly Game Portal2 = new()
    {
        Name = "Portal 2",
        NameId = "portal2",
        SteamId = 620
    };
    public static readonly Game Unknown = new()
    {
        Name = "Unknown Game",
        NameId = "unknown",
        SteamId = -1,
        IsUnknown = true
    };

    public string Name { get; private set; }
    public string NameId { get; private set; }
    public int SteamId { get; private set; }

    public bool IsUnknown { get; private set; }

    private Game()
    {
        IsUnknown = false;
        Name = string.Empty;
        NameId = string.Empty;
    }

    public static Game FromSteamId(int id)
    {
        if (id == Portal2.SteamId) return Portal2;
        else
        {
            Game game = (Game)Unknown.MemberwiseClone();
            game.SteamId = id;
            return game;
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is Game game) return Equals(game);
        return false;
    }
    public bool Equals(Game? other) => other is not null && SteamId == other.SteamId;
    public override int GetHashCode() => base.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Game a, Game b) => a.Equals(b);
    public static bool operator !=(Game a, Game b) => !a.Equals(b);
}
