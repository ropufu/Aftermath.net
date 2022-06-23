namespace Ropufu;

/// <summary>
/// Exposes all logging functionality of <see cref="Verbose{}"/>.
/// </summary>
public abstract class Logger<TSource>
    : Verbose<TSource>
    where TSource : class
{
    public new void Log(string message, MessageLevel level, TSource? source = null)
        => base.Log(message, level, source);

    public new void Log(ConsoleMessage<TSource> message)
        => base.Log(message);

    public new void Log(ConsoleMessage<TSource> message, TSource source)
        => base.Log(message, source);

    public new void LogRange(IEnumerable<ConsoleMessage<TSource>> messages)
        => base.LogRange(messages);

    public new void LogRange(IEnumerable<ConsoleMessage<TSource>> messages, TSource source)
        => base.LogRange(messages, source);

    public new void Clear()
        => base.Clear();

    public new void Clear(MessageLevel level)
        => base.Clear(level);
}
