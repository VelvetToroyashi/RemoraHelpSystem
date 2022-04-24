using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Remora.Commands.Extensions;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
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
            .AddSingleton(Mock.Of<IDiscordRestChannelAPI>())
            .AddScoped<TreeWalker>()
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
        var formatterMock = new Mock<IHelpFormatter>();

        formatterMock
            .Setup(fm => fm.GetTopLevelHelpEmbeds(It.IsAny<IEnumerable<IGrouping<string, IChildNode>>>()))
            .Returns(new IEmbed[] { new Embed() { Title = "Top Level Commands"} });

        var channelMock = new Mock<IDiscordRestChannelAPI>();
        
        var services = new ServiceCollection()
            .AddSingleton(formatterMock.Object)
            .AddSingleton(channelMock)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, string.Empty);
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock.Verify(fm => fm.GetTopLevelHelpEmbeds(It.IsAny<IEnumerable<IGrouping<string, IChildNode>>>()), Times.Once);
    }

    [Test]
    public async Task GroupReturnsSubcommands()
    {
        var formatterMock = new Mock<IHelpFormatter>();
        
        formatterMock
            .Setup(fm => fm.GetCommandHelp((IEnumerable<IChildNode>)It.IsAny<IEnumerable<IGrouping<string, IChildNode>>>()))
            .Returns(new IEmbed[] { new Embed() { Title = "Showing subcommands for group" } });
        
        var channelMock = new Mock<IDiscordRestChannelAPI>();
        
        var services = new ServiceCollection()
            .AddSingleton(formatterMock.Object)
            .AddSingleton(channelMock)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "group");
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock.Verify
        (
            fm => fm.GetCommandHelp
            (
                (IEnumerable<IChildNode>)It.Is<IEnumerable<IGrouping<string, IChildNode>>>
                (
                    s => s.Count() == 1 && 
                         s.First().Count() == 1 && 
                         s.First().First().Key == "command"
                )
            ),
            Times.Once
        );
    }

    [Test]
    public async Task ExecutableGroupUsesSubCommands()
    {
        var formatterMock = new Mock<IHelpFormatter>();
        
        formatterMock
            .Setup(fm => fm.GetCommandHelp((IEnumerable<IChildNode>)It.IsAny<IEnumerable<IGrouping<string, IChildNode>>>()))
            .Returns(new IEmbed[] { new Embed() { Title = "Showing subcommands for group" } });
        
        var channelMock = new Mock<IDiscordRestChannelAPI>();
        
        var services = new ServiceCollection()
            .AddSingleton(formatterMock.Object)
            .AddSingleton(channelMock)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "executable-group");
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock.Verify
        (
            fm => fm.GetCommandHelp
            (
                (IEnumerable<IChildNode>)It.Is<IEnumerable<IGrouping<string, IChildNode>>>
                (
                    s => s.Count() == 1 && 
                         s.First().Count() == 2 && 
                         s.First().First() is CommandNode && 
                         s.First().Last() is GroupNode
                )
            ),
            Times.Once
        );
    }
    
    [Test]
    public async Task SingleCommandReturnsSingleCommand()
    {
        var formatterMock = new Mock<IHelpFormatter>();
        
        formatterMock
            .Setup(fm => fm.GetCommandHelp(It.IsAny<IChildNode>()))
            .Returns( new Embed() { Title = "Showing single command" } );
        
        var channelMock = new Mock<IDiscordRestChannelAPI>();
        
        var services = new ServiceCollection()
            .AddSingleton(formatterMock.Object)
            .AddSingleton(channelMock)
            .BuildServiceProvider();
        
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            _serviceProvider.GetRequiredService<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "command");
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock.Verify
        (
            fm => fm.GetCommandHelp(It.Is<IChildNode>(c => c.Key == "command")),
            Times.Once
        );
    }

    [Test]
    public async Task CommandOverloadsHandledCorrectly()
    {
        var formatterMock = new Mock<IHelpFormatter>();
        
        formatterMock
            .Setup(fm => fm.GetCommandHelp((IEnumerable<IChildNode>)It.IsAny<IEnumerable<IGrouping<string,IChildNode>>>()))
            .Returns(new IEmbed[] { new Embed() { Title = "Showing subcommands for group" } });
        
        var channelMock = new Mock<IDiscordRestChannelAPI>();
        
        var services = new ServiceCollection()
            .AddSingleton(formatterMock.Object)
            .AddSingleton(channelMock.Object)
            .BuildServiceProvider();

        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            services,
            services.GetRequiredService<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "overload");
        
        
        Assert.IsTrue(result.IsSuccess);
        
        formatterMock.Verify
        (
            fm => fm.GetCommandHelp
            (
                (IEnumerable<IChildNode>)It.Is<IEnumerable<IGrouping<string, IChildNode>>>
                (
                    s => s.Count() == 1 && 
                         s.First().Count() == 2 && 
                         s.First().First() is CommandNode && 
                         s.First().Last() is CommandNode
                )
            ),
            Times.Once
        );
    }

    [Test]
    public async Task RequiresRegisteredFormatter()
    {
        var help = new CommandHelpService
        (
            _serviceProvider.GetRequiredService<TreeWalker>(),
            new ServiceCollection().BuildServiceProvider(),
            _serviceProvider.GetRequiredService<IDiscordRestChannelAPI>()
        );
        
        var result = await help.ShowHelpAsync(_channelID, "command");
        
        Assert.IsFalse(result.IsSuccess);
        Assert.IsInstanceOf<InvalidOperationError>(result.Error);
    }

}