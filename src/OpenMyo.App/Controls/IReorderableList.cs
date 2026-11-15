namespace OpenMyo.App.Controls;

/// <summary>A list whose items can be moved by index, used by drag-to-reorder in views.</summary>
public interface IReorderableList
{
    void MoveItem(int fromIndex, int toIndex);
}
