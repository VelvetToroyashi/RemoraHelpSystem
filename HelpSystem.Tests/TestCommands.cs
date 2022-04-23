using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Results;

namespace HelpSystem.Tests;

[ExcludeFromCodeCoverage]
public class TestCommands : CommandGroup
{
    [Command("command")]
    public async Task<IResult> Command() => default;
    
    
    [Command("overload")]
    public async Task<IResult> Overload(string arg) => default;
    
    [Command("overload")]
    public async Task<IResult> Overload(int arg) => default;
    
    [Command("aliased-command", "aliased")]
    public async Task<IResult> AliasedCommand() => default;
    
    [Group("group")]
    public class GroupOne : CommandGroup
    {
        [Command("command")]
        public async Task<IResult> Command() => default;
    }
    
    [Command("executable-group")]
    public async Task<IResult> ExecutableGroupCommand() => default;
    
    [Group("executable-group")]
    public class ExecutableGroup : CommandGroup
    {
        [Command("eg-command")]
        public async Task<IResult> Command() => default;
    }
    
    [Group("nested-group-1")]
    public class NestedGroupOne : CommandGroup
    {
        [Command("nested-command-1")]
        public async Task<IResult> Command() => default;
        
        [Group("nested-group-2")]
        public class NestedGroupTwo : CommandGroup
        {
            [Command("nested-command-2")]
            public async Task<IResult> Command() => default;
        }
    }
}