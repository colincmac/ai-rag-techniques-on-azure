namespace Showcase.Shared.AIExtensions;
public readonly struct SseEvent<T>
{
    /// <summary>Initializes a new instance of the <see cref="SseEvent{T}"/> struct.</summary>
    /// <param name="data">The event's payload.</param>
    public SseEvent(T data)
    {
        Data = data;
    }

    /// <summary>Gets the event's payload.</summary>
    public T Data { get; }

    /// <summary>Gets the event's type.</summary>
    public string? EventType { get; init; }

    /// <summary>Gets the event's identifier.</summary>
    public string? Id { get; init; }
}
