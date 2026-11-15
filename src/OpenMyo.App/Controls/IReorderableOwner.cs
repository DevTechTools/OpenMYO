namespace OpenMyo.App.Controls;

/// <summary>A list that lets its rows move or remove themselves.</summary>
public interface IReorderableOwner<in TRow>
{
    void Move(TRow row, int offset);

    void Remove(TRow row);
}
