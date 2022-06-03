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

    protected void LogError(string message, string? source = null)
        => _errorMessages.Add(new(message, ErrorLevel.Error, source));

    protected void LogWarning(string message, string? source = null)
        => _errorMessages.Add(new(message, ErrorLevel.Warning, source));

    protected void LogInformation(string message, string? source = null)
        => _errorMessages.Add(new(message, ErrorLevel.Information, source));

    protected void Log(string message, ErrorLevel level, string? source = null)
        => _errorMessages.Add(new(message, level, source));

    protected void Log(ErrorMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _errorMessages.Add(message);
    }

    protected void Log(IEnumerable<ErrorMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        foreach (ErrorMessage x in messages)
            _errorMessages.Add(x ?? throw new ArgumentException(Literals.ExpectedNotNullItems, nameof(messages)));
    }
}
