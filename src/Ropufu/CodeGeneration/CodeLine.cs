using System.Diagnostics.CodeAnalysis;

namespace Ropufu.CodeGeneration;

public class CodeLine
{
    private int _tabOffset = 0;
    private string _code = string.Empty;

    public CodeLine(string code = "", int tabOffset = 0)
    {
        this.Code = code;
        this.TabOffset = tabOffset;
    }

    public string Code
    {
        get => _code;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _code = value;
        }
    }

    public int TabOffset
    {
        get => _tabOffset;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _tabOffset = value;
        }
    }

    public CodeLine Format(object arg0)
        => new(string.Format(_code, arg0), _tabOffset);

    public CodeLine Format(object arg0, object arg1)
        => new(string.Format(_code, arg0, arg1), _tabOffset);

    public CodeLine Format(params object[] args)
        => new(string.Format(_code, args), _tabOffset);

    [return: NotNullIfNotNull("code")]
    public static implicit operator CodeLine?(string? code)
        => code is null ? null : new(code);
}
