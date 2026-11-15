using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace OpenMyo.App.Controls;

/// <summary>
/// Text that reads as a label until the user asks to rename it: double-click it, or press Enter or F2 on its
/// list row. Enter or leaving the box keeps the change; Escape restores the old text. A single click only
/// selects the row, so clicking a track never drops a caret into its title by accident.
/// </summary>
public sealed class InlineEditText : Grid
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<InlineEditText, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<InlineEditText, int>(nameof(MaxLength));

    private readonly TextBlock _label = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly PathIcon _hint = new() { Classes = { "editHint" }, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBox _box = new() { Classes = { "inline" }, IsVisible = false };
    private ListBoxItem? _row;

    public InlineEditText()
    {
        ColumnDefinitions = new ColumnDefinitions("*,Auto");
        Background = Brushes.Transparent; // hit-testable across the whole width
        _hint.Data = Application.Current?.FindResource("IconEdit") as Geometry;
        _label.Classes.Add("editLabel");
        SetColumn(_hint, 1);
        SetColumnSpan(_box, 2);
        Children.AddRange([_label, _hint, _box]);

        DoubleTapped += (_, e) => { BeginEdit(); e.Handled = true; };
        _box.KeyDown += OnBoxKeyDown;
        _box.LostFocus += (_, _) => EndEdit(commit: true);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public bool IsEditing => _box.IsVisible;

    public void BeginEdit()
    {
        _box.Text = Text;
        _box.MaxLength = MaxLength;
        _label.IsVisible = _hint.IsVisible = false;
        _box.IsVisible = true;
        _box.Focus();
        _box.SelectAll();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            _label.Text = Text;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _row = this.FindAncestorOfType<ListBoxItem>();
        _row?.AddHandler(KeyDownEvent, OnRowKeyDown, RoutingStrategies.Bubble);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _row?.RemoveHandler(KeyDownEvent, OnRowKeyDown);
        _row = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnRowKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsEditing && e.Source == _row && e.Key is Key.Enter or Key.F2)
        {
            BeginEdit();
            e.Handled = true;
        }
    }

    private void OnBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            EndEdit(commit: e.Key == Key.Enter);
            _row?.Focus();
            e.Handled = true;
        }
    }

    private void EndEdit(bool commit)
    {
        if (!IsEditing)
        {
            return;
        }

        if (commit && _box.Text != Text)
        {
            Text = _box.Text;
        }

        _box.IsVisible = false;
        _label.IsVisible = _hint.IsVisible = true;
    }
}
