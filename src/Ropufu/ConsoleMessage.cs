namespace Ropufu;

public enum MessageLevel
{
    Information = 0,
    Success,
    Warning,
    Error
}

public sealed class ConsoleMessage<TSource>
    where TSource : class
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

    public TSource? Source { get; init; }

    public MessageLevel Level { get; init; }

    public ConsoleMessage(string message, MessageLevel level, TSource? source = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        _message = message;
        this.Level = level;
        this.Source = source;
    }

    public void WriteToConsole()
    {
        ConsoleColor foregroundColor = Console.ForegroundColor;
        ConsoleColor backgroundColor = Console.BackgroundColor;
        Console.BackgroundColor = ConsoleColor.Black;
        switch (this.Level)
        {
            case MessageLevel.Information:
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("[Info]    ");
                break;
            case MessageLevel.Success:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[OK]      ");
                break;
            case MessageLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("[Warning] ");
                break;
            case MessageLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[Error]   ");
                break;
            default:
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"[{this.Level}] ");
                break;
        }

        Console.ForegroundColor = ConsoleColor.White;
        if (this.Source is not null)
            Console.Write($"{this.Source}: ");

        Console.BackgroundColor = backgroundColor;
        Console.ForegroundColor = foregroundColor;

        Console.Write(this.Message);
    }
}
