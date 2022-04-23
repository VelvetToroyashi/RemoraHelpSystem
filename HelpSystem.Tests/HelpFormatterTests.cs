using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Remora.Commands.Extensions;
using VTP.Remora.Commands.HelpSystem.Services;

namespace HelpSystem.Tests;

public partial class HelpFormatterTests
{
    private TreeWalker _treeWalker;
    private DefaultHelpFormatter _formatter;
    private IServiceProvider _services;
    
    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection()
            .AddCommands()
            .AddCommandTree()
            .WithCommandGroup<FormatterTestCommands>()
            .Finish()
            .AddSingleton<DefaultHelpFormatter>()
            .AddSingleton<TreeWalker>()
            .BuildServiceProvider();
        
        _formatter = _services.GetRequiredService<DefaultHelpFormatter>();
        _treeWalker = _services.GetRequiredService<TreeWalker>();
    }
}