using System.Diagnostics.CodeAnalysis;

namespace VTP.Remora.Commands.HelpSystem;

/// <summary>
/// Options related to the command help system.
/// </summary>
/// <param name="TreeName">The tree to search when looking for commands.</param>
/// <param name="DisplayMode">Whether or not to show commands regardless of if their conditions have been met.</param>
[ExcludeFromCodeCoverage]
public record HelpSystemOptions(string? TreeName, bool AlwaysShowCommands = false);