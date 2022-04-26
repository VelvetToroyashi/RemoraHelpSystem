using System.Diagnostics.CodeAnalysis;

namespace VTP.Remora.Commands.HelpSystem;

/// <summary>
/// Options related to the command help system.
/// </summary>
/// <param name="TreeName">The tree to search when looking for commands.</param>
/// <param name="DisplayMode">See <see cref="HelpConditionDisplayMode"/>; defaults to <see cref="HelpConditionDisplayMode.HideAll"/></param>
[ExcludeFromCodeCoverage]
public record HelpSystemOptions(string? TreeName, HelpConditionDisplayMode DisplayMode = HelpConditionDisplayMode.HideAll);