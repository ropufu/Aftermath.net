using System.Text.Json.Serialization;

namespace Ropufu.Json;

public class Verbose
{
    private readonly List<ErrorMessage> _errorMessages = new();

    [JsonIgnore]
    public IReadOnlyList<ErrorMessage> ErrorMessages => _errorMessages.AsReadOnly();

    public bool Has(ErrorLevel level)
    {
        foreach (ErrorMessage message in _errorMessages)
            if (message.Level == level)
                return true;

        return false;
    }

    protected void LogError(string message, JsonPointer? source = null)
        => _errorMessages.Add(new(message, ErrorLevel.Error, source?.ToString()));

    protected void LogWarning(string message, JsonPointer? source = null)
        => _errorMessages.Add(new(message, ErrorLevel.Warning, source?.ToString()));

    protected void LogInformation(string message, JsonPointer? source = null)
        => _errorMessages.Add(new(message, ErrorLevel.Information, source?.ToString()));

    protected void Log(string message, ErrorLevel level, JsonPointer? source = null)
        => _errorMessages.Add(new(message, level, source?.ToString()));

    protected void Log(ErrorMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _errorMessages.Add(message);
    }

    private void LogUnchecked(ErrorMessage message, JsonPointer source)
    {
        if (message.Source is null)
            _errorMessages.Add(new(message.Message, message.Level, source.ToString()));
        else
            _errorMessages.Add(new(message.Message, message.Level, source.Append(message.Source).ToString()));
    }

    protected void Log(ErrorMessage message, JsonPointer source)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(source);

        this.LogUnchecked(message, source);
    }

    protected void Log(IEnumerable<ErrorMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        foreach (ErrorMessage x in messages)
            if (x is null)
                throw new ArgumentException(Literals.ExpectedNotNullItems, nameof(messages));
            else
                _errorMessages.Add(x);
    }

    protected void Log(IEnumerable<ErrorMessage> messages, JsonPointer source)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(source);

        foreach (ErrorMessage x in messages)
            if (x is null)
                throw new ArgumentException(Literals.ExpectedNotNullItems, nameof(messages));
            else
                this.LogUnchecked(x, source);
    }

    protected void Clear() => _errorMessages.Clear();

    protected void Clear(ErrorLevel level)
    {
        for (int i = 0; i < _errorMessages.Count; ++i)
        {
            if (_errorMessages[i].Level == level)
            {
                _errorMessages.RemoveAt(i);
                --i;
            } // if (...)
        } // for (...)
    }
}
