namespace Fintacharts.AssetTracker.Shared.Events;

using System.Collections.Concurrent;
using Interfaces;

public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Delegate>> _handlers = new();

    public void Publish<T>(T @event)
    {
        if (_handlers.TryGetValue(typeof(T), out var handlers))
        {
            foreach (var handler in handlers.Cast<Action<T>>())
            {
                handler(@event);
            }
        }
    }

    public void Subscribe<T>(Action<T> handler)
    {
        var handlers = _handlers.GetOrAdd(typeof(T), _ => new ConcurrentBag<Delegate>());
        handlers.Add(handler);
    }
}