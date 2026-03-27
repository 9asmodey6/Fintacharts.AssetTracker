namespace Fintacharts.AssetTracker.Shared.Exceptions;

public class IncompleteBatchException(string message, IEnumerable<Guid> missingIds) 
    : Exception(message)
{
    public IEnumerable<Guid> MissingIds { get; } = missingIds;
}