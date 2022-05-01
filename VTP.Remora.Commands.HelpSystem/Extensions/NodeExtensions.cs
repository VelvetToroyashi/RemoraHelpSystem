using System.Diagnostics.CodeAnalysis;
using Remora.Commands;
using Remora.Commands.Trees.Nodes;

namespace VTP.Remora.Commands.HelpSystem;

[ExcludeFromCodeCoverage]
public static class GroupNodeExtensions
{
    /// <summary>
    /// Returns the description of a <see cref="GroupNode"/> if set, otherwise <c>null</c>.
    /// </summary>
    /// <param name="gn">The group node to get the description from.</param>
    /// <returns>The description of the group node if set, otherwise <c>null</c>.</returns>
    public static string? GetDescription(this GroupNode gn)
    {
        if (string.Equals("No description set.", gn.Description, StringComparison.OrdinalIgnoreCase))
            return null;
        
        return string.IsNullOrEmpty(gn.Description) ? null : gn.Description;

    }
    
    /// <summary>
    /// Returns the description of a <see cref="GroupNode"/> if set, otherwise <c>null</c>.
    /// </summary>
    /// <param name="cn">The command node to get the description from.</param>
    /// <returns>The description of the command node if set, otherwise <c>null</c>.</returns>
    public static string? GetDescription(this CommandNode cn)
    {
        if (string.Equals(Constants.DefaultDescription, cn.Shape.Description, StringComparison.OrdinalIgnoreCase))
            return null;
        
        return string.IsNullOrEmpty(cn.Shape.Description) ? null : cn.Shape.Description;
    }
}