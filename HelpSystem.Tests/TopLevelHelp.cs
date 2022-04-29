using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Remora.Commands.Extensions;
using VTP.Remora.Commands.HelpSystem.Services;

namespace HelpSystem.Tests;

public partial class HelpFormatterTests
{
    public class TopLevelHelp
    {
        [Test]
        public void TopLevelHelpShowsAllCommandsCorrectly()
        {
            var services = new ServiceCollection()
                .AddCommands()
                .AddCommandTree()
                .WithCommandGroup<Tests.TopLevelHelp>()
                .Finish()
                .AddSingleton<TreeWalker>()
                .BuildServiceProvider();
            
            var walker = services.GetRequiredService<TreeWalker>();
            
            var formatter = new DefaultHelpFormatter();

            var result = formatter.GetTopLevelHelpEmbeds(walker.FindNodes(null).GroupBy(x => x.Key));
            
            Assert.AreEqual(1, result.Count());
            
            var embed = result.First();
            
            Assert.AreEqual("`command-1` `command-2` `command-3` `group-1` `group-2*` ", embed.Description.Value);
        }
    }
}