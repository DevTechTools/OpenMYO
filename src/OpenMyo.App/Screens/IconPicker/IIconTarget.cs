using OpenMyo.Core.Icons;
using OpenMyo.Core.Text;

namespace OpenMyo.App.Screens.IconPicker;

/// <summary>Anything the icon picker can assign an icon to (a chapter, a draft track, or several at once).</summary>
public interface IIconTarget
{
    /// <summary>Shown in the picker header.</summary>
    string Title { get; }

    /// <summary>What the picker searches for when it opens.</summary>
    string SearchText => IconQuery.FromTitle(Title);

    /// <summary>False when the target still needs an icon; see <see cref="IconTargetSequence"/>.</summary>
    bool HasIcon { get; }

    void AssignIcon(DisplayIcon icon);
}
