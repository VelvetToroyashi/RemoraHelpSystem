using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Remora.Commands.Trees.Nodes;

namespace HelpSystem.Tests;

public partial class HelpFormatterTests
{
    [Test]
    public void WorksWithOverloads()
    {
        var command = _treeWalker.FindNodes("overload");

        var embeds = _formatter.GetCommandHelp(command);
        
        Assert.AreEqual(2, embeds.Count());
        
        Assert.AreEqual("Help for overload (overload 1 of 2)", embeds.First().Title.Value);
        Assert.AreEqual("Help for overload (overload 2 of 2)", embeds.Last().Title.Value);
        
        // TODO: Check parameters? Description?
    }
    
    [Test]
    public void DisplaysCorrectGroupInformationInSingleEmbed()
    {
        var command = _treeWalker.FindNodes("standalone-group");

        var embeds = _formatter.GetCommandHelp(command);
        
        Assert.AreEqual(1, embeds.Count());
        Assert.AreEqual("Showing sub-command help for standalone-group", embeds.First().Title.Value);
    }
    
    [Test]
    public void WorksWithSingleChildGroup()
    {
        var command = _treeWalker.FindNodes("standalone-group");

        var embed = _formatter.GetCommandHelp(command).Single();
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`command`\r", description[2]);
    }

    [Test]
    public void WorksWithMultiChildGroup()
    {
        var command = _treeWalker.FindNodes("multi-child-group");
        
        var embed = _formatter.GetCommandHelp(command).Single();
        
        var description = embed.Description.Value.Split('\n');

        Assert.AreEqual("`command-1`\r", description[2]);
        Assert.AreEqual("`command-2`\r", description[3]);
    }

    [Test]
    public void WorksWithParameterlessExecutableGroup()
    {
        var command = _treeWalker.FindNodes("parameterless-executable-group");
        
        var embed = _formatter.GetCommandHelp(command).Single();

        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("This group can be executed like a command without parameters.", description[2]);
    }
    
    [Test]
    public void WorksWithParameterizedExecutableGroup()
    {
        var command = _treeWalker.FindNodes("parameterized-executable-group");
        
        var embed = _formatter.GetCommandHelp(command).Single();

        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("This group can be executed like a command.", description[2]);
        
        Assert.AreEqual("`<parameter>`\r", description[3]);
    }
    
    [Test]
    public void WorksWithOverloadedParameterizedExecutableGroup()
    {
        var command = _treeWalker.FindNodes("overloaded-parameterized-executable-group");
        
        var embed = _formatter.GetCommandHelp(command).Single();
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("This group can be executed like a command without parameters.", description[2]);
        
        Assert.AreEqual("`<parameter>`\r", description[3]);
    }

    [Test]
    public void DescribedExecutableGroupUsesGroupDescription()
    {
        var command = _treeWalker.FindNodes("described-executable-group");
        
        var embed = _formatter.GetCommandHelp(command).Single();
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("Group description\r", description[0]);
    }

    [Test]
    public void DescribedExecutableGroupUsesCommandDescriptionCorrectly()
    {
        var command = _treeWalker.FindNodes("described-executable-group-2");
        
        var embed = _formatter.GetCommandHelp(command).Single();
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("Command description\r", description[0]);
    }
    
    [Test]
    public void FallsBackToCommandDescription()
    {
        var command = _treeWalker.FindNodes("parameterized-executable-group");
        
        var embed = _formatter.GetCommandHelp(command).Single();
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("No description set.\r", description[0]);
    }

    [Test]
    public void HandlesComplexGroupsCorrectly()
    {
        var command = _treeWalker.FindNodes("complex-group");
        
        var embeds = _formatter.GetCommandHelp(command).Single();
        
        var description = embeds.Description.Value.Split('\n');
        
        Assert.AreEqual("`command`\r", description[2]);
        Assert.AreEqual("`overload`\r", description[3]);
        Assert.AreEqual("`nested-executable-group*`\r", description[4]);
    }
}