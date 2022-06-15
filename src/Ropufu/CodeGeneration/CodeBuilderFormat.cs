namespace Ropufu.CodeGeneration;

public class CodeBuilderFormat
{
    private int _tabSize = 4;
    private string _newLineSequence = "\n";

    public char TabSymbol { get; set; } = ' ';

    public int TabSize
    {
        get => _tabSize;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _tabSize = value;
        }
    }

    public string NewLineSequence
    {
        get => _newLineSequence;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _newLineSequence = value;
        }
    }
}
