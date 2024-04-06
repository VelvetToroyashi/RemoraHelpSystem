using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Commands.Conditions;
using Remora.Commands.Results;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace VTP.Remora.Commands.HelpSystem.Services;

[PublicAPI]
public class CommandHelpService
(
    TreeWalker treeWalker,
    IServiceProvider services,
    IOptions<HelpSystemOptions> options,
    IDiscordRestChannelAPI channels
)
: ICommandHelpService
{
    private readonly HelpSystemOptions _options = options.Value;


    public async Task<Result> ShowHelpAsync(Snowflake channelID, string? commandName = null, string? treeName = null)
    {
        var nodes = treeWalker.FindNodes(commandName, treeName);

        if (!_options.AlwaysShowCommands)
        {
            var evaluation = await EvaluateNodeConditionsAsync(nodes);

            if (!string.IsNullOrEmpty(commandName) && evaluation.UnsatisfiedCondition is {} unsatisfied)
                return Result.FromError(new ConditionNotSatisfiedError("One or more conditions were not satisfied.", unsatisfied));

            nodes = evaluation.Nodes.ToArray();
        }
        
        if (!nodes.Any())
            return Result.FromError(new NotFoundError($"No command with the name \"{commandName}\" was found."));

        var formatter = services.GetService<IHelpFormatter>();
        
        if (formatter is null)
            return Result.FromError(new InvalidOperationError("Help was invoked, but no formatter was registered."));

#pragma warning disable CS8509 // 'switch expression is not exhaustive'; heuristically unreachable condition
        IEnumerable<IEmbed> embeds = (nodes.Count, string.IsNullOrEmpty(commandName), nodes.FirstOrDefault() is IParentNode) switch
        {
            (> 1, true,  _) => formatter.GetTopLevelHelpEmbeds(nodes.GroupBy(n => n.Key)),
            (> 1, false, _) => formatter.GetCommandHelp(nodes),
            (  1, _, false) =>  [ formatter.GetCommandHelp(nodes.Single()) ],
            (  1, _,  true) => formatter.GetCommandHelp(nodes)
        };
    #pragma warning restore CS8509

        var sendResult = await channels.CreateMessageAsync(channelID, embeds: embeds.ToArray());

        return sendResult.IsSuccess ? Result.FromSuccess() : Result.FromError(sendResult.Error);
    }

    public async Task<(IEnumerable<IChildNode> Nodes, ConditionAttribute? UnsatisfiedCondition)> EvaluateNodeConditionsAsync(IReadOnlyList<IChildNode> nodes)
    {
        var successfulNodes = new HashSet<IChildNode>();
        ConditionAttribute? unsatisfiedCondition = null;

        foreach (var node in nodes)
        {
            List<ConditionAttribute> conditions = [];

            switch (node)
            {
                case CommandNode cn:
                    conditions.AddRange(cn.GroupType.GetCustomAttributes<ConditionAttribute>());
                    conditions.AddRange(cn.CommandMethod.GetCustomAttributes<ConditionAttribute>());
                    break;
                case GroupNode gn:
                    conditions.AddRange(gn.GroupTypes.SelectMany(gt => gt.GetCustomAttributes<ConditionAttribute>()));
                    break;
            }

            if (!conditions.Any())
            {
                successfulNodes.Add(node);
                continue;
            }

            foreach (var setCondition in conditions)
            {
                var conditionType = typeof(ICondition<>).MakeGenericType(setCondition.GetType());
                var conditionMethod = conditionType.GetMethod(nameof(ICondition<ConditionAttribute>.CheckAsync));
                
                var conditionServices = services
                                        .GetServices(conditionType)
                                        .Where(c => c is not null)
                                        .Cast<ICondition>()
                                        .ToArray();
                
                if (!conditionServices.Any())
                    throw new InvalidOperationException($"Command was marked with \"{conditionType.Name}\", but no service was registered to handle it.");

                foreach (var condition in conditionServices)
                {
                    var result = await (ValueTask<Result>) conditionMethod!.Invoke(condition, [setCondition, CancellationToken.None])!;

                    if (result.IsSuccess)
                    {
                        successfulNodes.Add(node);
                    }
                    else
                    {
                        successfulNodes.Remove(node);
                        unsatisfiedCondition ??= setCondition;
                        goto next;
                    }
                }

                continue;
                
                next:
                break;
            }
        }

        return (successfulNodes, unsatisfiedCondition);
    }
}