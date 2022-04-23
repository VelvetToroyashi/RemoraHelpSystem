using NUnit.Framework;

namespace HelpSystem.Tests;

public partial class HelpFormatterTests
{
    [Test]
    public void ShowsCorrectCommandName()
    {
        var command = _treeWalker.FindNodes("parameterless")[0];

        var embed = _formatter.GetCommandHelp(command);
        
        Assert.AreEqual("Help for parameterless", embed.Title.Value);
    }
    
    [Test]
    public void WorksForParmeterlessCommand()
    {
        var command = _treeWalker.FindNodes("parameterless")[0];
        
        var embed = _formatter.GetCommandHelp(command);

        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("This command can be used without any parameters.\r", description[1]);
    }
    
    [Test]
    public void WorksWithParameterizedCommand()
    {
        var command = _treeWalker.FindNodes("parameterized")[0];
        
        var embed = _formatter.GetCommandHelp(command);

        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`<parameter>` No description set.\r", description[2]);
    }

    [Test]
    public void WorksWithOptionalParameter()
    {
        var command = _treeWalker.FindNodes("optional-parameterized")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`[parameter]` No description set.\r", description[2]);
    }

    [Test]
    public void WorksWithParameterWithDescription()
    {
        var command = _treeWalker.FindNodes("described-parameter")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`<parameter>` description\r", description[2]);
    }

    [Test]
    public void WorksWithMultiParmeterCommand()
    {
        var command = _treeWalker.FindNodes("multi-parameterized")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`<parameter1>` No description set.\r", description[2]);
        Assert.AreEqual("`<parameter2>` No description set.\r", description[4]);
    }
    
    [Test]
    public void WorksWithMultiParameterWithOptional()
    {
        var command = _treeWalker.FindNodes("multi-parameterized-optional")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`<parameter1>` No description set.\r", description[2]);
        Assert.AreEqual("`[parameter2]` No description set.\r", description[4]);
    }

    [Test]
    public void WorksWithShortNamedOption()
    {
        var command = _treeWalker.FindNodes("short-named-option")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`<-o parameter>` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithLongNamedOption()
    {
        var command = _treeWalker.FindNodes("long-named-option")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`<--option parameter>` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithOptionWithShortAndLongName()
    {
        var command = _treeWalker.FindNodes("short-long-named-option")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`<-o/--option parameter>` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithOptionalShortNamedOption()
    {
        var command = _treeWalker.FindNodes("optional-short-named-option")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`[-o parameter]` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithOptionalLongNamedOption()
    {
        var command = _treeWalker.FindNodes("optional-long-named-option")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`[--option parameter]` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithOptionalShortAndLongNamedOption()
    {
        var command = _treeWalker.FindNodes("optional-short-long-named-option")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`[-o/--option parameter]` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithShortNamedSwitch()
    {
        var command = _treeWalker.FindNodes("short-named-switch")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`[-s]` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithLongNamedSwitch()
    {
        var command = _treeWalker.FindNodes("long-named-switch")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`[--switch]` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithShortAndLongNamedSwitch()
    {
        var command = _treeWalker.FindNodes("short-long-named-switch")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("`[-s/--switch]` No description set.\r", description[2]);
    }
    
    [Test]
    public void WorksWithDescriptionlessCommand()
    {
        var command = _treeWalker.FindNodes("descriptionless")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("No description set.\r", description[0]);
    }

    [Test]
    public void WorksWithDescriptedCommand()
    {
        var command = _treeWalker.FindNodes("descriptioned")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("Descriptioned command\r", description[0]);
    }
    
    [Test]
    public void WorksWithPermissionGatedCommand()
    {
        var command = _treeWalker.FindNodes("permissioned")[0];
        
        var embed = _formatter.GetCommandHelp(command);
        
        var description = embed.Description.Value.Split('\n');
        
        Assert.AreEqual("This command requires the following permissions: SendMessages\r", description[1]);
    }
    
}