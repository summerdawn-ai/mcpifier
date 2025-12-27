using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace Summerdawn.Mcpifier.Server;

/// <summary>
/// Extension methods for <see cref="Command"/>.
/// </summary>
internal static class CommandExtensions
{
    /// <summary>
    /// Adds the specified help text to the end of the help output for the given command.
    /// </summary>
    /// <param name="command">The command to add help text to.</param>
    /// <param name="text">The help text to add.</param>
    public static void AddHelpText(this Command command, string text)
    {
        var helpOption = command.Options.OfType<HelpOption>().FirstOrDefault() ?? throw new InvalidOperationException("Command has no help option.");

        helpOption.Action = new CustomHelpAction((HelpAction)helpOption.Action!, text);
    }

    /// <summary>
    /// Custom help action to add text to the end of help output.
    /// </summary>
    internal class CustomHelpAction(HelpAction innerAction, string helpText) : SynchronousCommandLineAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            int result = innerAction.Invoke(parseResult);

            Console.WriteLine(helpText);

            return result;
        }
    }
}