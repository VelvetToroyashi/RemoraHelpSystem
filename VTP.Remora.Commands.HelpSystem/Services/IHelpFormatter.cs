using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;

namespace VTP.Remora.Commands.HelpSystem.Services;

public interface IHelpFormatter
{
    /// <summary>
    /// Creates an embed for a help screen of a single command.
    /// </summary>
    /// <param name="command">The command that was found.</param>
    /// <returns>An embed displaying relevant information about the command.</returns>
    IEmbed GetCommandHelp(IChildNode command);
    
    /// <summary>
    /// Creates one or more embeds for a help screen for the children of a group.
    /// </summary>
    /// <param name="subCommands">The child commands, grouped by name.</param>
    /// <param name="executable">Whether the parent group is considered execurable.</param>
    /// <returns>One or more embeds displaying relevant information about the given commands.</returns>
    IEnumerable<IEmbed> GetSubCommandEmbeds(IEnumerable<IGrouping<string, IChildNode>> subCommands, bool executable);
    
    /// <summary>
    /// Creates one or more embeds for a help screen showing the top-level commands.
    /// </summary>
    /// <param name="commands">The top-level commands of the searched tree.</param>
    /// <returns>One or more embeds displaying help for the given commands.</returns>
    IEnumerable<IEmbed> GetTopLevelHelpEmbeds(IEnumerable<IGrouping<string, IChildNode>> commands);
}