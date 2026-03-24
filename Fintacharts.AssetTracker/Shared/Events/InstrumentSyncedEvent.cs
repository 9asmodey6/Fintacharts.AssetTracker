namespace Fintacharts.AssetTracker.Shared.Events;

public sealed class InstrumentsSyncedEvent
{
    public IReadOnlyList<string> InstrumentIds { get; }

    public InstrumentsSyncedEvent(IReadOnlyList<string> instrumentIds)
    {
        InstrumentIds = instrumentIds;
    }
}