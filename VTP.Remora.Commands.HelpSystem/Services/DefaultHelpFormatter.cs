using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Remora.Commands.Attributes;
using Remora.Commands.Extensions;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using SwitchAttribute = System.Diagnostics.SwitchAttribute;

namespace VTP.Remora.Commands.HelpSystem.Services;

public class DefaultHelpFormatter : IHelpFormatter
{

    public IEmbed GetCommandHelp(IChildNode command) => null;
    
    public IEnumerable<IEmbed> GetSubCommandEmbeds(IEnumerable<IGrouping<string, IChildNode>> subCommands)
    {
        yield break;
    }
    
    public IEnumerable<IEmbed> GetTopLevelHelpEmbeds(IEnumerable<IGrouping<string, IChildNode>> commands)
    {
        var sorted = commands.OrderBy(x => x.Key);

        var sb = new StringBuilder();
        
        foreach (var group in sorted)
        {
            if (group.Count() is 1 || !group.Any(g => g is IParentNode))
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
            builder.Append(parameter.IsOmissible() ? "`[`" : "`<`");
            
            if (parameter.Parameter.GetCustomAttribute<SwitchAttribute>() is not null)
                builder.Append("--");
            
            builder.Append(parameter.Parameter.Name);

            if (parameter.Parameter.GetCustomAttribute<OptionAttribute>() is { } oa)
            {
                if (oa.ShortName is not null && oa.LongName is not null)
                    builder.Append($"{oa.ShortName}/{oa.LongName}");
                else builder.AppendLine(oa.ShortName?.ToString() ?? oa.LongName);
            }
            
            if (parameter.IsOmissible())
                builder.Append("`]`");
            else
                builder.Append("`>`");
            
            builder.AppendLine($" {(string.IsNullOrEmpty(parameter.Description) ? "No description" : parameter.Description)}");
        }        
    }

    private void AddRequiredPermissions(StringBuilder builder, IChildNode node)
    {
        if (node.GetType().GetCustomAttribute<RequireDiscordPermissionAttribute>() is { } rpa)
            builder.AppendLine($"This command requires the following permissions: {string.Join(", ", rpa.Permissions)}");
    }

}