using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using OpenMyo.Core.Library;

namespace OpenMyo.App.Controls;

/// <summary>A text block that shows search matches in bold accent text, so people see why a result matched.</summary>
public sealed class HighlightedTextBlock : TextBlock
{
    private const string MatchBrushKey = "AccentTextBrush";

    public static readonly StyledProperty<IReadOnlyList<TextSegment>?> SegmentsProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IReadOnlyList<TextSegment>?>(nameof(Segments));

    static HighlightedTextBlock() =>
        SegmentsProperty.Changed.AddClassHandler<HighlightedTextBlock>((block, _) => block.Rebuild());

    public IReadOnlyList<TextSegment>? Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    // Keep the normal TextBlock styles (secondary text, fonts) from the theme.
    protected override Type StyleKeyOverride => typeof(TextBlock);

    private void Rebuild()
    {
        var inlines = new InlineCollection();
        foreach (var segment in Segments ?? [])
        {
            var run = new Run(segment.Text);
            if (segment.IsMatch)
            {
                run.FontWeight = FontWeight.SemiBold;
                run.Bind(TextElement.ForegroundProperty, this.GetResourceObservable(MatchBrushKey));
            }

            inlines.Add(run);
        }

        Inlines = inlines;
    }
}
