using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Remora.Commands.Conditions;
using Remora.Commands.Extensions;
using Remora.Commands.Results;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Results;
using Remora.Rest.Core;
using Remora.Results;
using VTP.Remora.Commands.HelpSystem;
using VTP.Remora.Commands.HelpSystem.Services;

namespace HelpSystem.Tests;

public class HelpServiceTests
{
    private readonly Snowflake _channelID = new(1);
    
    private CommandHelpService _help;
    private IServiceProvider _serviceProvider;
    
    [SetUp]
    public void Setup()
    {
        _serviceProvider = new ServiceCollection()
            .AddCommands()
            .AddCommandTree()
            .WithCommandGroup<TestCommands>()
            .Finish()
            .AddSingleton(Substitute.For<IDiscordRestChannelAPI>())
            .AddScoped<TreeWalker>()
            .Configure<HelpSystemOptions>(h => h.AlwaysShowCommands = true)
            .AddScoped<CommandHelpService>()
            .BuildServiceProvider();
        
        _help = _serviceProvider.GetService<CommandHelpService>();
    }

    [Test]
    public void UnknownCommandReturnsError()
    {
        var result = _help.ShowHelpAsync(default, "unknown").Result; // Sync path, don't care
        
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("No command with the name \"unknown\" was found.", result.Error.Message);
    }

    [Test]
    public async Task EmptyInputInvokesTopLevelCommandHelp()
    {
        var formatterMock = Substitute.For<IHelpFormatter>();

        formatterMock
            .GetTopLevelHelpEmbeds(Arg.Any<IEnumerable<IGrouping<string, IChildNode>>>())
            .Returns(new IEmbed[] { new Embed { Title = "Showing top level help" } });

        var services = new ServiceCollection()
            .AddSingleton(formatterMock)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, string.Empty);
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock.Received().GetTopLevelHelpEmbeds(Arg.Any<IEnumerable<IGrouping<string, IChildNode>>>());
    }

    [Test]
    public async Task GroupReturnsSubcommands()
    {
        var formatterMock = Substitute.For<IHelpFormatter>();
        
        formatterMock
            .GetCommandHelp(Arg.Any<IEnumerable<IChildNode>>())
            .Returns(new IEmbed[] { new Embed { Title = "Showing subcommands for group" } });
        
        var services = new ServiceCollection()
            .AddSingleton(formatterMock)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "group");
        
        Assert.IsTrue(result.IsSuccess);
        
        // This is technically wrong, see commit f1dec1fa
        // or the comment in this GetCommandHelp below
        formatterMock
            .Received()
            .GetCommandHelp
            (
                Arg.Is<IEnumerable<IChildNode>>
                (
                    c => c.Count() == 2 && c.All(n => n is CommandNode)
                )
            );
    }

    [Test]
    public async Task ExecutableGroupUsesSubCommands()
    {
        var formatterMock = Substitute.For<IHelpFormatter>();
        
        formatterMock
            .GetCommandHelp(Arg.Any<IEnumerable<IChildNode>>())
            .Returns(new IEmbed[] { new Embed { Title = "Showing subcommands for executable group" } });
        
        var services = new ServiceCollection()
            .AddSingleton(formatterMock)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "executable-group");
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock
            .Received()
            .GetCommandHelp
            (
                Arg.Is<IEnumerable<IChildNode>>
                (
                    c => c.Count() == 2 && c.All(n => n is CommandNode)
                )
            );
    }
    
    [Test]
    public async Task SingleCommandReturnsSingleCommand()
    {
        var formatterMock = Substitute.For<IHelpFormatter>();
        
        formatterMock
            .GetCommandHelp(Arg.Any<IEnumerable<IChildNode>>())
            .Returns(new IEmbed[] { new Embed { Title = "Showing help for single command" } });
        
        var services = new ServiceCollection()
            .AddSingleton(formatterMock)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "command");
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock
            .Received()
            .GetCommandHelp
            (
                Arg.Is<IEnumerable<IChildNode>>
                (
                    c => c.Count() == 1 && c.First() is CommandNode
                )
            );
    }

    [Test]
    public async Task CommandOverloadsHandledCorrectly()
    {
        var formatterMock = Substitute.For<IHelpFormatter>();
        
        formatterMock
            .GetCommandHelp(Arg.Any<IEnumerable<IChildNode>>())
            .Returns(new IEmbed[] { new Embed { Title = "Showing overloads" } });

        var services = new ServiceCollection()
            .AddSingleton(formatterMock)
            .BuildServiceProvider();

        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "overload");
        
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock
            .Received()
            .GetCommandHelp
            (
                Arg.Is<IEnumerable<IChildNode>>
                (
                     c => c.Count() == 2 &&
                     c.All(n => n is CommandNode)
                )
            );
    }

    [Test]
    public async Task RequiresRegisteredFormatter()
    {
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            new ServiceCollection().BuildServiceProvider(),
            _serviceProvider.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "command");
        
        Assert.IsFalse(result.IsSuccess);
        Assert.IsInstanceOf<InvalidOperationError>(result.Error);
    }

    [Test]
    public async Task UnregisteredConditionReturnsError()
    {
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<IHelpFormatter>())
            .Configure<HelpSystemOptions>(help => help.AlwaysShowCommands = false)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );

        Assert.ThrowsAsync<InvalidOperationException>(async () => await help.ShowHelpAsync(_channelID, "conditioned"));
    }
    
    [Test]
    public async Task UnregisteredGroupReturnsError()
    {
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<IHelpFormatter>())
            .Configure<HelpSystemOptions>(help => help.AlwaysShowCommands = false)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );
        
        Assert.ThrowsAsync<InvalidOperationException>(async () => await help.ShowHelpAsync(_channelID, "conditioned-group"));
    }

    [Test]
    public async Task CorrectlyChecksMultiTypeGroupConditions()
    {
        var conditionMock = Substitute.For<ICondition<RequireDiscordPermissionAttribute>>();
        
        conditionMock
            .CheckAsync(Arg.Any<RequireDiscordPermissionAttribute>(), Arg.Any<CancellationToken>())
            .Returns(Result.FromSuccess());

        var services = new ServiceCollection()
            .AddCommands()
            .AddCommandTree()
            .WithCommandGroup<TestCommands2>()
            .WithCommandGroup<TestCommands3>()
            .Finish()
            .AddSingleton<TreeWalker>()
            .AddSingleton(conditionMock)
            .AddSingleton(Substitute.For<IHelpFormatter>())
            .Configure<HelpSystemOptions>(help => help.AlwaysShowCommands = false)
            .BuildServiceProvider();

        var help = new CommandHelpService
        (
            services.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "group2");
        
        Assert.IsFalse(result.IsSuccess);
        
        Assert.IsInstanceOf<ConditionNotSatisfiedError>(result.Error);
    }

    [Test]
    public async Task ConditionlessGroupIsAlwaysReturned()
    {
        var conditionMock = Substitute.For<ICondition<RequireDiscordPermissionAttribute>>();
        
        conditionMock
            .CheckAsync(Arg.Any<RequireDiscordPermissionAttribute>(), Arg.Any<CancellationToken>())
            .Returns(Result.FromSuccess());
        
        var services = new ServiceCollection()
            .AddCommands()
            .AddCommandTree()
            .WithCommandGroup<TestCommands>()
            .Finish()
            .AddSingleton<TreeWalker>()
            .AddSingleton(conditionMock)
            .AddSingleton(Substitute.For<IHelpFormatter>())
            .Configure<HelpSystemOptions>(help => help.AlwaysShowCommands = false)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            services.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );

        var result = await help.EvaluateNodeConditionsAsync(services.GetRequiredService<TreeWalker>().FindNodes("group"));
        
        Assert.AreEqual(1, result.Nodes.Count());
    }

    [Test]
    public async Task EvaluatesCommandTypeConditionsCorrectly()
    {
        var conditionMock = Substitute.For<ICondition<RequireDiscordPermissionAttribute>>();
        
        conditionMock.CheckAsync(Arg.Any<RequireDiscordPermissionAttribute>(), Arg.Any<CancellationToken>())
                     .Returns(Result.FromError(new PermissionDeniedError(), Result.FromSuccess()));
        
        var services = new ServiceCollection()
            .AddCommands()
            .AddCommandTree()
            .WithCommandGroup<TestCommands4>()
            .Finish()
            .AddSingleton<TreeWalker>()
            .AddSingleton(conditionMock)
            .AddSingleton(Substitute.For<IHelpFormatter>())
            .Configure<HelpSystemOptions>(help => help.AlwaysShowCommands = false)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            services.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );

        var result = await help.EvaluateNodeConditionsAsync(services.GetRequiredService<TreeWalker>().FindNodes("conditioned-group-2 command"));
        
        Assert.IsEmpty(result.Nodes);
        
#pragma warning disable CA2012
        _ = conditionMock
            .Received()
            .CheckAsync
            (
                Arg.Is<RequireDiscordPermissionAttribute>(c => c.Permissions[0] == DiscordPermission.ManageChannels),
                Arg.Any<CancellationToken>()
            );
        
        _ = conditionMock
            .DidNotReceive()
            .CheckAsync
            (
                Arg.Is<RequireDiscordPermissionAttribute>(c => c.Permissions[0] == DiscordPermission.ManageRoles),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore CA2012
    }

    [Test]
    public async Task EvaluatesCommandMethodConditionsCorrectly()
    {
        var conditionMock = Substitute.For<ICondition<RequireDiscordPermissionAttribute>>();
        
        conditionMock.CheckAsync(Arg.Any<RequireDiscordPermissionAttribute>(), Arg.Any<CancellationToken>())
                     .Returns(Result.FromSuccess(), Result.FromError(new PermissionDeniedError()));
        
        var services = new ServiceCollection()
            .AddCommands()
            .AddCommandTree()
            .WithCommandGroup<TestCommands4>()
            .Finish()
            .AddSingleton<TreeWalker>()
            .AddSingleton(conditionMock)
            .AddSingleton(Substitute.For<IHelpFormatter>())
            .Configure<HelpSystemOptions>(help => help.AlwaysShowCommands = false)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            services.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );

        var result = await help.EvaluateNodeConditionsAsync(services.GetRequiredService<TreeWalker>().FindNodes("conditioned-group-2 command"));
        
        Assert.IsEmpty(result.Nodes);
        
#pragma warning disable CA2012 // 'ValueTasks should be awaited' Shut up, Roslyn.
        _ = conditionMock.Received()
                     .CheckAsync
                    (
                        Arg.Is<RequireDiscordPermissionAttribute>(c => c.Permissions[0] == DiscordPermission.ManageChannels),
                        Arg.Any<CancellationToken>()
                    );
        
        _ = conditionMock.Received()
                     .CheckAsync
                    (
                        Arg.Is<RequireDiscordPermissionAttribute>(c => c.Permissions[0] == DiscordPermission.ManageRoles),
                        Arg.Any<CancellationToken>()
                    );
#pragma warning restore CA2012
    }
    
    [Test]
    public async Task ShowsAllTopLevelHelpWhenUsingShowAllCommands()
    {
        var conditionMock = Substitute.For<ICondition<RequireDiscordPermissionAttribute>>();
        
        conditionMock
            .CheckAsync(Arg.Any<RequireDiscordPermissionAttribute>(), Arg.Any<CancellationToken>())
            .Returns(Result.FromSuccess());
        
        var formatterMock = Substitute.For<IHelpFormatter>();
            
        var services = new ServiceCollection()
            .AddCommands()
            .AddCommandTree()
            .WithCommandGroup<TopLevelHelp.Uncategorized>()
            .Finish()
            .AddSingleton<TreeWalker>()
            .AddSingleton(conditionMock)
            .AddSingleton(formatterMock)
            .Configure<HelpSystemOptions>(help => help.AlwaysShowCommands = true)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            services.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );

        var result = await help.ShowHelpAsync(_channelID);
            
        Assert.IsTrue(result.IsSuccess);
            
        formatterMock.Received()
                     .GetTopLevelHelpEmbeds(Arg.Is<IEnumerable<IGrouping<string, IChildNode>>>(g => g.Count() == 5));
    }
    
    [Test]
    public async Task CorrectlyHidesConditionedCommandsForTopLevelHelp()
    {
        var conditionMock = Substitute.For<ICondition<RequireDiscordPermissionAttribute>>();
        
        conditionMock
            .CheckAsync(Arg.Any<RequireDiscordPermissionAttribute>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(Result.FromError(new PermissionDeniedError()));
        
        var formatterMock = Substitute.For<IHelpFormatter>();
            
        var services = new ServiceCollection()
            .AddCommands()
            .AddCommandTree()
            .WithCommandGroup<TopLevelHelp.Uncategorized>()
            .Finish()
            .AddSingleton<TreeWalker>()
            .AddSingleton(conditionMock)
            .AddSingleton(formatterMock)
            .Configure<HelpSystemOptions>(help => help.AlwaysShowCommands = false)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            services.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IOptions<HelpSystemOptions>>(),
            Substitute.For<IDiscordRestChannelAPI>()
        );

        var result = await help.ShowHelpAsync(_channelID);
            
        Assert.IsTrue(result.IsSuccess);

        formatterMock.Received()
                     .GetTopLevelHelpEmbeds(Arg.Is<IEnumerable<IGrouping<string, IChildNode>>>(g => g.Count() == 4));
    }
}