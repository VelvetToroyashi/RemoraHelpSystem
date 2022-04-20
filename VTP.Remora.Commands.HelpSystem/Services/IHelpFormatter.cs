using System.Collections.Generic;
using System.Linq;
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
    /// <remarks>
    /// This should never be invoked with a group node, as the implementation may not properly handle it.
    /// Instead, the callee should invoke <see cref="GetSubCommandEmbeds"/>.
    /// </remarks>
    IEmbed GetCommandHelp(IChildNode command);
    
    /// <summary>
    /// Creates one or more embeds for a help screen for the children of a group.
    /// </summary>
    /// <param name="subCommands">The child commands, grouped by name.</param>
    /// <param name="parent">The parent command node of the subcommands.</param>
    /// <param name="isExecutable">Whether the parent command is executable.</param>
    /// <returns>One or more embeds displaying relevant information about the given commands.</returns>
    IEnumerable<IEmbed> GetSubCommandEmbeds(IEnumerable<IGrouping<string, IChildNode>> subCommands, IParentNode parent, bool isExecutable);
    
    /// <summary>
    /// Creates one or more embeds for a help screen showing the top-level commands.
    /// </summary>
    /// <param name="commands">The top-level commands of the searched tree.</param>
    /// <returns>One or more embeds displaying help for the given commands.</returns>
    IEnumerable<IEmbed> GetTopLevelHelpEmbeds(IEnumerable<IGrouping<string, IChildNode>> commands);
}