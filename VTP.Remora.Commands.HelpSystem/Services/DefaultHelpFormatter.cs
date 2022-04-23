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
            sb.AppendLine("No description provided.");

        
        AddRequiredPermissions(sb, command);
        AddCommandUsage(sb, command);

        var embed = GetBaseEmbed() with
        {
            Title = $"Help for {command.Key}",
            Description = sb.ToString()
        };

        return embed;
    }
    
    public IEnumerable<IEmbed> GetSubCommandEmbeds(IEnumerable<IGrouping<string, IChildNode>> subCommands)
    {
        if (subCommands.Count() is 1 && subCommands.First().All(sc => sc is not IParentNode))
        {
            var overloads = subCommands.First().ToArray();

            for (int i = 0; i < overloads.Length; i++)
                yield return (GetCommandHelp(overloads[i]) as Embed) with { Title = $"Help for {overloads[0].Key} (overload {i + 1} of {overloads.Length})" };

            yield break;
        }
        
        if (subCommands.Count() is 1)
        {
            var group = subCommands.First(sc => sc is IParentNode) as IParentNode;
            var executable = subCommands.Where(c => c is not IParentNode);
            
            var sb = new StringBuilder();
            
            foreach (var scGroup in group.Children.GroupBy(c => c.Key))
            {
                if (scGroup.Count() > 1 && scGroup.Any(sc => sc is IParentNode))
                    sb.AppendLine($"`{scGroup.Key}*`");
                else 
                    sb.AppendLine($"`{scGroup.Key}`");
            }
            var embed = GetBaseEmbed() with
            {
                Title = $"Showing sub-command help for {(group as IChildNode).Key}",
                Description = sb.ToString()
            };
            
            yield return embed;
        }
        else
        {
            var sb = new StringBuilder();
            
            foreach (var scGroup in subCommands)
            {
                if (scGroup.Count() > 1 && scGroup.Any(sc => sc is IParentNode))
                    sb.AppendLine($"`{scGroup.Key}*`");
                else 
                    sb.AppendLine($"`{scGroup.Key}`");
            }
            
            var embed = GetBaseEmbed() with
            {
                Title = $"Showing sub-command help for {(subCommands.First().First().Parent as IChildNode).Key}",
                Description = sb.ToString()
            };
            
            yield return embed;
        }
        
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
        if (node.GetType().GetCustomAttribute<RequireDiscordPermissionAttribute>() is { } rpa)
            builder.AppendLine($"This command requires the following permissions: {string.Join(", ", rpa.Permissions)}");
    }

}