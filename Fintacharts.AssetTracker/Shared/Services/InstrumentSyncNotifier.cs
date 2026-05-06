namespace Fintacharts.AssetTracker.Shared.Services;

public class InstrumentSyncNotifier
{
    private readonly object _lock = new();
    private CancellationTokenSource _cts = new();

    /// <summary>
    /// Returns a token that gets cancelled when instruments change.
    /// PriceUpdateWorker links this into its session CTS.
    /// Each call after NotifyInstrumentsChanged() returns a fresh (non-cancelled) token.
    /// </summary>
    public CancellationToken ReconnectToken
    {
        get
        {
            lock (_lock)
                return _cts.Token;
        }
    }

    /// <summary>
    /// Called by InstrumentSyncWorker when the set of instruments has changed.
    /// Cancels the current token (triggering WS reconnect) and prepares a fresh one.
    /// </summary>
    public void NotifyInstrumentsChanged()
    {
        lock (_lock)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }
}