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

        if (string.IsNullOrEmpty(commandName))
        {
            embeds = formatter.GetTopLevelHelpEmbeds(nodes.GroupBy(n => n.Key));
        }
        else if (nodes.Count > 1)
        {
            embeds = formatter.GetSubCommandEmbeds(nodes.GroupBy(n => n.Key));
        }
        else
        {
            if (nodes.First() is IParentNode pn)
                embeds = formatter.GetSubCommandEmbeds(pn.Children.GroupBy(n => n.Key));
            else
                embeds = new[] { formatter.GetCommandHelp(nodes.First()) };
        }
        
        return default;
    }
}