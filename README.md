## VTP.RemoraHelpSystem

### What is it?

This repo serves as the basis for an innovative, modular, and extensible 
library of components to create a help system for [Remora.Discord](https://github.com/Nihlus/Remora.Discord).

This repo is built off of the findings in my [proof-of-concept](https://github.com/VelvetThePanda/HelpSystemPOC)

### What does it do?

In short, it provides components that can be pieced together to construct a full help system, including tree searching, 
message formatting, metadata handling, and even dispatching the help message.

### How do I use it?

Firstly add `VTP.Remora.Commands.HelpSystem` from NuGet to your project.

Then, add the help system:

```cs
var services = new ServiceCollection();

services.AddHelpSystem();
```

This will register the following into the container:

- TreeWalker
- HelpCommand
- IHelpFormatter
- ICommandHelpService


`TreeWalker` is a generic component that simply walks a given tree and returns all the matching nodes.

`ICommandHelpService` is an interface that is used to retrieve help for command(s).

`IHelpFormatter` is an interface used by the default implementation of `ICommandHelpService` to create embeds based on the retrieved nodes. 

`HelpCommand` is a command that simply invokes `ICommandHelpService.ShowHelpAsync` based on the user's query.

### I use custom-named trees, what do I do?

That's fine, simply pass the name of the tree you want to register help for:

```cs
    services.AddHelpSystem("my_custom_tree");
```

### I want to group my commands, what do I do?

Grouping commands is easy, simply mark your command with the `[Category("my cateogory")]` attribute.

Then, on your service collection:

```csharp
services
    .Configure<HelpSystemOptions>(options => 
    {
        options.CommandCategories.Add("my category");
    });
```

Now any commands marked with `[Category("my category")]` will be grouped under the category "my category".
