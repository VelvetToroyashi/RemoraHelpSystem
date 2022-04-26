namespace VTP.Remora.Commands.HelpSystem;

/// <summary>
/// An enum describing how help should be handled when commands have conditions.
/// </summary>
public enum HelpConditionDisplayMode
{
    /// <summary>
    /// Help for all commands and subcommands should always be shown, regardless
    /// of whether the conditions are met or not.
    /// </summary>
    ShowAlways,
    
    /// <summary>
    /// Hides help for commands and subcommands that have conditions that are not met.
    /// </summary>
    HideUnapplicable,
    
    /// <summary>
    /// If a condition cannot be satisfied, no help is shown at all.
    ///
    /// For top-level commands, by default <see cref="HideUnapplicable"/>
    /// is applied unless set to <see cref="ShowAlways"/>
    /// </summary>
    HideAll
}