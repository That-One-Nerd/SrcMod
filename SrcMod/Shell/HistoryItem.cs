namespace SrcMod.Shell;

public struct HistoryItem
{
    public required Action action;
    public required string name;
    public DateTime timestamp;

    public HistoryItem()
    {
        timestamp = DateTime.Now;
    }

    public void Invoke() => action.Invoke();

    public override string ToString() => $"{timestamp:MM/dd/yyyy HH:mm:ss} | {name}";
}
