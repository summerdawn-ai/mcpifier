namespace Summerdawn.Mcpifier.Abstractions;

/// <summary>
/// Provides an abstraction over standard input and output streams.
/// </summary>
public interface IStdio
{
    /// <summary>
    /// Gets the standard input stream.
    /// </summary>
    public Stream GetStandardInput();

    /// <summary>
    /// Gets the standard output stream.
    /// </summary>
    public Stream GetStandardOutput();
}
