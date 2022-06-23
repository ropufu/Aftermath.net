using System.Text;

namespace Ropufu.CodeGeneration;

public sealed class CodeBuilder
{
    private class BuilderBlock : IDisposable
    {
        private bool _disposedValue;
        private readonly CodeBuilder _builder;

        public BuilderBlock(CodeBuilder builder)
        {
            this._builder = builder;
            ++this._builder.TabLevel;
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                    --this._builder.TabLevel;

                this._disposedValue = true;
            } // if (...)
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    private readonly List<CodeLine> _lines = new();

    public int TabLevel { get; private set; }

    public bool IsEmpty => _lines.Count == 0;

    public CodeBuilder()
    {
    }

    public CodeBuilder(int capacity)
        => _lines = new(capacity: capacity);

    /// <summary>
    /// All lines within a code block will have extra indentation.
    /// </summary>
    public IDisposable NewCodeBlock()
        => new BuilderBlock(this);

    /// <summary>
    /// Adds an empty line to the collection.
    /// </summary>
    public CodeBuilder Append()
    {
        _lines.Add(new("", this.TabLevel));
        return this;
    }

    /// <summary>
    /// Adds a line to the collection.
    /// </summary>
    public CodeBuilder Append(string value, int tabOffset = 0)
    {
        ArgumentNullException.ThrowIfNull(value);

        _lines.Add(new(value, this.TabLevel + tabOffset));
        return this;
    }

    public CodeBuilder Append(CodeLine value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _lines.Add(new(value.Code, this.TabLevel + value.TabOffset));
        return this;
    }

    public CodeBuilder Append(IEnumerable<CodeLine> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (CodeLine x in values)
            if (x is null)
                throw new ArgumentException("Code lines cannot contain null elements.", nameof(values));
            else
                _lines.Add(new(x.Code, this.TabLevel + x.TabOffset));

        return this;
    }

    public CodeBuilder Append(CodeBuilder value)
    {
        ArgumentNullException.ThrowIfNull(value);

        foreach (CodeLine x in value._lines)
            this.Append(x);

        return this;
    }

    /// <summary>
    /// Joins all non-empty pieces of code separated by an extra line break.
    /// </summary>
    public CodeBuilder AppendJoin(params CodeBuilder[] values)
        => this.AppendJoin(values as IEnumerable<CodeBuilder>);

    /// <summary>
    /// Joins all non-empty pieces of code separated by an extra line break.
    /// </summary>
    public CodeBuilder AppendJoin(IEnumerable<CodeBuilder> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        List<CodeBuilder> nonEmpty = new();

        foreach (CodeBuilder x in values)
            if (!x.IsEmpty)
                nonEmpty.Add(x);

        bool isFirst = true;

        foreach (CodeBuilder x in nonEmpty)
        {
            if (!isFirst)
                this.Append();

            this.Append(x);
            isFirst = false;
        } // foreach (...)

        return this;
    }

    public override string ToString()
        => this.ToString(new());

    public string ToString(CodeBuilderFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);

        StringBuilder builder = new();

        foreach (CodeLine x in _lines)
        {
            int offset = format.TabSize * x.TabOffset;

            builder
                .Append(format.TabSymbol, repeatCount: offset)
                .Append(x.Code)
                .Append(format.NewLineSequence);
        } // foreach (...)

        return builder.ToString();
    }
}
