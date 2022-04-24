using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace VTP.Remora.Commands.HelpSystem.Services;

public class CommandHelpService : ICommandHelpService
{
    private readonly TreeWalker _treeWalker;
    private readonly IServiceProvider _services;
    private readonly IDiscordRestChannelAPI _channels;
    
    public CommandHelpService(TreeWalker treeWalker, IServiceProvider services, IDiscordRestChannelAPI channels)
    {
        _treeWalker = treeWalker;
        _services   = services;
        _channels   = channels;
    }

    public async Task<Result> ShowHelpAsync(Snowflake channelID, string? commandName = null, string? treeName = null)
    {
        var nodes = _treeWalker.FindNodes(commandName, treeName);

        if (!nodes.Any())
            return Result.FromError(new NotFoundError($"No command with the name \"{commandName}\" was found."));

        var formatter = _services.GetService<IHelpFormatter>();
        
        if (formatter is null)
            return Result.FromError(new InvalidOperationError("Help was invoked, but no formatter was registered."));

        IEnumerable<IEmbed> embeds;

        embeds = (nodes.Count, string.IsNullOrEmpty(commandName)) switch
        {
            (> 1, true) => formatter.GetTopLevelHelpEmbeds(nodes.GroupBy(n => n.Key)),
            (> 1, false) => formatter.GetCommandHelp(nodes),
            (1, _) => new [] {formatter.GetCommandHelp(nodes.Single()) }
        };

        var sendResult = await _channels.CreateMessageAsync(channelID, embeds: embeds.ToArray());

        return sendResult.IsSuccess ? Result.FromSuccess() : Result.FromError(sendResult.Error);
    }
}