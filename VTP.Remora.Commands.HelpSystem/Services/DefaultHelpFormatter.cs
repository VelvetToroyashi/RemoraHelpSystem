using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Remora.Commands.Attributes;
using Remora.Commands.Extensions;
using Remora.Commands.Groups;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;

namespace VTP.Remora.Commands.HelpSystem.Services;

public class DefaultHelpFormatter : IHelpFormatter
{

    public IEmbed GetCommandHelp(IChildNode command)
    {
        var sb = new StringBuilder();

        var casted = (CommandNode)command;

        if (!string.IsNullOrEmpty(casted.Shape.Description))
            sb.AppendLine(casted.Shape.Description);
        else
            sb.AppendLine("No description set.");

        
        AddRequiredPermissions(sb, command);
        AddCommandUsage(sb, command);

        var embed = GetBaseEmbed() with
        {
            Title = $"Help for {command.Key}",
            Description = sb.ToString()
        };

        return embed;
    }
    
    public IEnumerable<IEmbed> GetCommandHelp(IEnumerable<IChildNode> subCommands)
    {
        Embed embed = null!;
        
        var sb = new StringBuilder();

        if (subCommands.Count() is 1)
        {
            if (subCommands.Single() is not IParentNode pn)
            {
                yield return GetCommandHelp(subCommands.Single());
                yield break;
            }
            else
            {
                sb.AppendLine((pn as GroupNode).Description ?? "No description provided.");
                sb.AppendLine();
                
                var grouped = pn.Children.GroupBy(x => x.Key);

                foreach (var command in grouped)
                {
                    if (command.Count() > 1 && command.Any(sc => sc is IParentNode))
                        sb.AppendLine($"`{command.Key}*`");
                    else
                        sb.AppendLine($"`{command.Key}`");
                }

                embed = GetBaseEmbed() with
                {
                    Title = $"Showing sub-command help for {subCommands.Single().Key}",
                    Description = sb.ToString()
                };

                yield return embed;
                yield break;
            }
        }
        
        // Bug: Callee may pass children of a group, or the group itself.
        // We only support the latter in this implementation, but this is not
        // clearly documented. TODO: Add add check for naming??
        if (!subCommands.OfType<IParentNode>().Any())
        {
            var sca = subCommands.ToArray();
            
            for (int i = 0; i < sca.Length; i++)
                yield return (GetCommandHelp(sca[i]) as Embed) with { Title = $"Help for {sca[0].Key} (overload {i + 1} of {sca.Length})" };

            yield break;
        }
        
        // If we need to deal with overloaded groups, it's actually pretty simple.
        // var children = subCommands.OfType<IParentNode>().SelectMany(x => x.Children);
        // var parent = subCommands.OfType<IParentNode>().First();
        // Then, just use the children as you would normally. The reaosn this isn't done
        // by default is because it's somewhat niche? But the code is there in case changes
        // need to be made. There's also the performance impact of re-iterating more than we
        // have to, but we're using LINQ, so allocations > speed anyways.
        var group = subCommands.First(sc => sc is IParentNode) as GroupNode;

        // This makes the assumption that there are no overloaded groups,
        // which is impossible to do without backtracking anwyay.
        var executable = subCommands.Where(sc => sc is not IParentNode).Cast<CommandNode>();

        sb.AppendLine(group.GetDescription() ?? executable.FirstOrDefault(cn => cn.Shape.Description is not null)?.Shape.Description ?? "No description set.");
        sb.AppendLine();
        
        AddGroupCommandUsage(sb, executable);

        var gsc = group.Children.GroupBy(x => x.Key);
        
        foreach (var scGroup in gsc)
        {
            if (scGroup.Count() > 1 && scGroup.Any(sc => sc is IParentNode))
                sb.AppendLine($"`{scGroup.Key}*`");
            else 
                sb.AppendLine($"`{scGroup.Key}`");
        }
        
        embed = GetBaseEmbed() with
        {
            Title = $"Showing sub-command help for {(group as IChildNode).Key}",
            Description = sb.ToString()
        };
        
        yield return embed;

        yield break;
    }
    
    public IEnumerable<IEmbed> GetTopLevelHelpEmbeds(IEnumerable<IGrouping<string, IChildNode>> commands)
    {
        var sorted = commands.OrderBy(x => x.Key);

        var sb = new StringBuilder();
        
        foreach (var group in sorted)
        {
            if (group.Count() is 1 || group.All(g => g is not IParentNode))
                sb.AppendLine($"`{group.Key}` ");
            else
                sb.AppendLine($"`{group.Key}*` ");
        }

        var embed = GetBaseEmbed() with
        {
            Title = "All Commands",
            Description = sb.ToString(),
            Footer = new EmbedFooter("Specify a command for more information. Commands with \"*\" are groups that can be used like commands."),
        };
        
        yield return embed;
    }

    private Embed GetBaseEmbed() => new() { Colour = Color.DodgerBlue };

    private void AddGroupCommandUsage(StringBuilder builder, IEnumerable<IChildNode> overloads)
    {
        var casted = overloads.Cast<CommandNode>().ToArray();
        
        builder.Append($"This group can be executed like a command");

        if (casted.Any(ol => !ol.Shape.Parameters.Any()))
            builder.Append(" without parameters");
        
        builder.Append(".\n");

        foreach (var overload in casted)
        {
            if (!overload.Shape.Parameters.Any())
                continue;

            var localBuilder = new StringBuilder();

            localBuilder.Append('`');

            foreach (var parameter in overload.Shape.Parameters)
            {
                localBuilder.Append(parameter.IsOmissible() ? "[" : "<");

                char? shortName = null;
                string longName = null;

                var named = false;
                var isSwitch = false;

                if (parameter.Parameter.GetCustomAttribute<SwitchAttribute>() is { } sa)
                {
                    named = true;
                    isSwitch = true;

                    shortName = sa.ShortName;
                    longName = sa.LongName;
                }

                if (parameter.Parameter.GetCustomAttribute<OptionAttribute>() is { } oa)
                {
                    named = true;

                    shortName = oa.ShortName;
                    longName = oa.LongName;
                }

                if (named)
                {
                    if (shortName is not null && longName is not null)
                    {
                        localBuilder.Append($"-{shortName}/--{longName}");
                    }
                    else
                    {
                        if (shortName is not null)
                            localBuilder.Append($"-{shortName}");
                        else
                            localBuilder.Append($"--{longName}");
                    }

                    if (!isSwitch)
                        localBuilder.Append(' ');
                }

                if (!isSwitch)
                    localBuilder.Append(parameter.Parameter.Name);

                if (parameter.IsOmissible())
                    localBuilder.Append("]");
                else
                    localBuilder.Append(">");

                localBuilder.Append(' ');
            }
            
            localBuilder[^1] = '`';
            
            builder.AppendLine(localBuilder.ToString());
            builder.AppendLine();

            localBuilder.Clear();
        }
    }
    
    private void AddCommandUsage(StringBuilder builder, IChildNode command)
    {
        if (command is not CommandNode cn)
            return;

        if (!cn.Shape.Parameters.Any())
        {
            builder.AppendLine("This command can be used without any parameters.");
            return;
        }
        
        foreach (var parameter in cn.Shape.Parameters)
        {
            builder.AppendLine();
            builder.Append(parameter.IsOmissible() ? "`[" : "`<");

            char? shortName = null;
            string longName = null;

            var named = false;
            var isSwitch = false;

            if (parameter.Parameter.GetCustomAttribute<SwitchAttribute>() is { } sa)
            {
                named = true;
                isSwitch = true;

                shortName = sa.ShortName;
                longName = sa.LongName;
            }
            
            if (parameter.Parameter.GetCustomAttribute<OptionAttribute>() is { } oa)
            {
                named = true;

                shortName = oa.ShortName;
                longName = oa.LongName;
            }

            if (named)
            {
                if (shortName is not null && longName is not null)
                {
                    builder.Append($"-{shortName}/--{longName}");
                }
                else
                {
                    if (shortName is not null)
                        builder.Append($"-{shortName}");
                    else
                        builder.Append($"--{longName}");
                }
                
                if (!isSwitch)
                    builder.Append(' ');
            }

           if (!isSwitch)
                builder.Append(parameter.Parameter.Name);
            
            if (parameter.IsOmissible())
                builder.Append("]`");
            else
                builder.Append(">`");
            
            builder.AppendLine($" {(string.IsNullOrEmpty(parameter.Description) ? "No description" : parameter.Description)}");
        }        
    }

    private void AddRequiredPermissions(StringBuilder builder, IChildNode node)
    {
        if (node is not CommandNode cn)
            return;
        
        if ((cn.GroupType.GetCustomAttribute<RequireDiscordPermissionAttribute>() ??
            cn.CommandMethod.GetCustomAttribute<RequireDiscordPermissionAttribute>()) is {} rpa)
            builder.AppendLine($"This command requires the following permissions: {string.Join(", ", rpa.Permissions)}");
    }

}