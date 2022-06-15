namespace Ropufu.Json;

public sealed class JsonLogger
    : Verbose
{
    public new void LogError(string message, JsonPointer? source = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        base.LogError(message, source);
    }

    public new void LogWarning(string message, JsonPointer? source = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        base.LogWarning(message, source);
    }

    public new void LogInformation(string message, JsonPointer? source = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        base.LogInformation(message, source);
    }

    public new void Log(string message, ErrorLevel level, JsonPointer? source = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        base.Log(message, level, source);
    }

    public new void Log(ErrorMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        base.Log(message);
    }

    public new void Log(ErrorMessage message, JsonPointer source)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(source);
        base.Log(message, source);
    }

    public new void Log(IEnumerable<ErrorMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        base.Log(messages);
    }

    public new void Log(IEnumerable<ErrorMessage> messages, JsonPointer source)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(source);
        base.Log(messages, source);
    }

    public new void Clear() => base.Clear();

    public new void Clear(ErrorLevel level) => base.Clear(level);
}
