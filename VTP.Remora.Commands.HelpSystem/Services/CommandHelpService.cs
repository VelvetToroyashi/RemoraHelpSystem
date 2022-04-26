﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    private readonly HelpSystemOptions _options;
    private readonly IDiscordRestChannelAPI _channels;
    
    
    public CommandHelpService
    (
        TreeWalker treeWalker,
        IServiceProvider services,
        IOptions<HelpSystemOptions> options,
        IDiscordRestChannelAPI channels
    )
    {
        _treeWalker = treeWalker;
        _services   = services;
        _options    = options.Value;
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

        embeds = (nodes.Count, string.IsNullOrEmpty(commandName), nodes.FirstOrDefault() is IParentNode) switch
        {
            (> 1, true, _)  => formatter.GetTopLevelHelpEmbeds(nodes.GroupBy(n => n.Key)),
            (> 1, false, _) => formatter.GetCommandHelp(nodes),
            (1, _, false)   => new [] {formatter.GetCommandHelp(nodes.Single()) },
            (1, _, true)    => formatter.GetCommandHelp(nodes)
        };

        var sendResult = await _channels.CreateMessageAsync(channelID, embeds: embeds.ToArray());

        return sendResult.IsSuccess ? Result.FromSuccess() : Result.FromError(sendResult.Error);
    }
}