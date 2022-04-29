using Remora.Rest.Core;
using Remora.Results;

namespace VTP.Remora.Commands.HelpSystem.Services;

public interface ICommandHelpService
{
    /// <summary>
    /// Shows help for a specified command, or shows all top-level commands if no command is specified. 
    /// </summary>
    /// <param name="channelID">The ID of the channel to send help to.</param>
    /// <param name="commandName">The name of the command to display help for.</param>
    /// <param name="treeName">The optional tree name to search through.</param>
    /// <returns>A result of the operation.</returns>
    Task<Result> ShowHelpAsync(Snowflake channelID, string? commandName = null, string? treeName = null);
}