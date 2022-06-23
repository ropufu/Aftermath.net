using System.Text.Json.Serialization;

namespace Ropufu;

/// <summary>
/// Provides logging functionality for private use.
/// </summary>
public abstract class Verbose<TSource>
    where TSource : class
{
    private readonly List<ConsoleMessage<TSource>> _consoleMessages = new();

    protected abstract TSource Compose(TSource a, TSource b);

    [JsonIgnore]
    public IReadOnlyList<ConsoleMessage<TSource>> Messages => _consoleMessages.AsReadOnly();

    public bool Has(MessageLevel level)
    {
        foreach (ConsoleMessage<TSource> message in _consoleMessages)
            if (message.Level == level)
                return true;

        return false;
    }

    protected void Log(string message, MessageLevel level, TSource? source = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        _consoleMessages.Add(new(message, level, source));
    }

    protected void Log(ConsoleMessage<TSource> message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _consoleMessages.Add(message);
    }

    private void LogUnchecked(ConsoleMessage<TSource> message, TSource outerSource)
    {
        if (message.Source is null)
            _consoleMessages.Add(new(message.Message, message.Level, outerSource));
        else
            _consoleMessages.Add(new(message.Message, message.Level, this.Compose(outerSource, message.Source)));
    }

    protected void Log(ConsoleMessage<TSource> message, TSource outerSource)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(outerSource);

        this.LogUnchecked(message, outerSource);
    }

    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException">Items should not be null.</exception>
    protected void LogRange(IEnumerable<ConsoleMessage<TSource>> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        foreach (ConsoleMessage<TSource> x in messages)
            if (x is null)
                throw new ArgumentException("Items should not be null.", nameof(messages));
            else
                _consoleMessages.Add(x);
    }

    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException">Items should not be null.</exception>
    protected void LogRange(IEnumerable<ConsoleMessage<TSource>> messages, TSource outerSource)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(outerSource);

        foreach (ConsoleMessage<TSource> x in messages)
            if (x is null)
                throw new ArgumentException("Items should not be null.", nameof(messages));
            else
                this.LogUnchecked(x, outerSource);
    }

    protected void Clear()
        => _consoleMessages.Clear();

    protected void Clear(MessageLevel level)
    {
        for (int i = 0; i < _consoleMessages.Count; ++i)
        {
            if (_consoleMessages[i].Level == level)
            {
                _consoleMessages.RemoveAt(i);
                --i;
            } // if (...)
        } // for (...)
    }
}
