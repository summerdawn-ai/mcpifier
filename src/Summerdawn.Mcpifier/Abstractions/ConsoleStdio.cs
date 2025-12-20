namespace Summerdawn.Mcpifier.Abstractions;

/// <summary>
/// Production implementation of <see cref="IStdio"/> that uses <see cref="Console"/> streams.
/// </summary>
public class ConsoleStdio : IStdio
{
    /// <inheritdoc />
    public Stream GetStandardInput() => Console.OpenStandardInput();

    /// <inheritdoc />
    public Stream GetStandardOutput() => Console.OpenStandardOutput();
}
