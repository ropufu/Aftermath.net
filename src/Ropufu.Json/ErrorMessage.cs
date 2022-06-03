namespace Ropufu.Json;

public enum ErrorLevel
{
    Information = 0,
    Warning,
    Error
}

public sealed class ErrorMessage
{
    private string _message;

    public string Message
    {
        get => _message;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _message = value;
        }
    }

    public string? Source { get; init; }

    public ErrorLevel Level { get; init; }

    public ErrorMessage(string message, ErrorLevel level = ErrorLevel.Error, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        _message = message;
        this.Level = level;
        this.Source = source;
    }

    public void WriteToConsole()
    {
        ConsoleColor foregroundColor = Console.ForegroundColor;
        switch (this.Level)
        {
            case ErrorLevel.Information:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[OK] ");
                break;
            case ErrorLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("[Warning] ");
                break;
            case ErrorLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[Error] ");
                break;
            default:
                Console.Write($"[{this.Level}] ");
                break;
        }
        Console.ForegroundColor = foregroundColor;

        if (this.Source is not null)
            Console.Write($"@{this.Source}: ");

        Console.Write(this.Message);
    }
}
