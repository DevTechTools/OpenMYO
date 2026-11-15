using System.Globalization;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Draws a 16x16 number icon (PNG) for numbers Yoto does not publish, matching Yoto's own set: the same
/// rounded 7x12 digits with 2-pixel strokes as Yoto's 21–30, and the same colour cycle (track 1 yellow,
/// 2 sky blue, 3 coral … repeating every nine). Three-digit numbers use a narrow 4x12 cut so they fit.
/// </summary>
public static class NumberIconPainter
{
    public const int MaxNumber = 999;
    private const int Size = 16;
    private const int GlyphHeight = 12;
    private const int Gap = 2;

    // Yoto's number colours, in order; number n uses Palette[(n - 1) % 9].
    private static readonly uint[] Palette =
        [0xFFDE40, 0x41C0F0, 0xEB5E44, 0xA1C519, 0xD69FC8, 0xE53215, 0xF8AE11, 0x44B481, 0x3778BC];

    // One string per digit, rows separated by spaces; '#' is ink.
    private static readonly string[] WideDigits =
    [
        ".#####. ####### ##...## ##...## ##...## ##...## ##...## ##...## ##...## ##...## ####### .#####.",
        "....##. ...###. ..####. ..####. ....##. ....##. ....##. ....##. ....##. ....##. ....##. ....##.",
        ".#####. ####### ##...## ##...## .....## ....### ...###. ..###.. .###... ###.... ####### #######",
        ".#####. ####### ##...## .....## ....### ..####. ..####. ....### .....## ##...## ####### .#####.",
        "....##. ...###. ..####. .#####. ###.##. ##..##. ####### ####### ....##. ....##. ....##. ....##.",
        "####### ####### ##..... ##..... ##..... ######. ####### .....## .....## ##...## ####### .#####.",
        ".#####. ####### ##...## ##..... ##..... ######. ####### ##...## ##...## ##...## ####### .#####.",
        "####### ####### .....## ....### ....##. ...###. ...##.. ..###.. ..##... .###... .##.... .##....",
        ".#####. ####### ##...## ##...## ##...## .#####. ####### ##...## ##...## ##...## ####### .#####.",
        ".#####. ####### ##...## ##...## ##...## ####### .###### .....## .....## ##...## ####### .#####.",
    ];

    private static readonly string[] NarrowDigits =
    [
        ".##. #..# #..# #..# #..# #..# #..# #..# #..# #..# #..# .##.",
        "..#. .##. #.#. ..#. ..#. ..#. ..#. ..#. ..#. ..#. ..#. ..#.",
        ".##. #..# ...# ...# ..#. ..#. .#.. .#.. #... #... #... ####",
        ".##. #..# ...# ...# ...# .##. ...# ...# ...# ...# #..# .##.",
        "..#. .##. .##. #.#. #.#. #.#. #### ..#. ..#. ..#. ..#. ..#.",
        "#### #... #... #... ###. ...# ...# ...# ...# ...# #..# .##.",
        ".##. #..# #... #... #... ###. #..# #..# #..# #..# #..# .##.",
        "#### ...# ...# ..#. ..#. ..#. .#.. .#.. .#.. .#.. .#.. .#..",
        ".##. #..# #..# #..# #..# .##. #..# #..# #..# #..# #..# .##.",
        ".##. #..# #..# #..# #..# #..# .### ...# ...# ...# #..# .##.",
    ];

    /// <summary>Returns the PNG bytes for <paramref name="number"/> (1–999).</summary>
    public static byte[] Paint(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, MaxNumber);

        var digits = number.ToString(CultureInfo.InvariantCulture);
        var font = digits.Length <= 2 ? WideDigits : NarrowDigits;
        var glyphWidth = font[0].IndexOf(' ', StringComparison.Ordinal);
        var width = (digits.Length * glyphWidth) + ((digits.Length - 1) * Gap);
        var ink = ToRgba(Palette[(number - 1) % Palette.Length]);
        var pixels = new byte[Size * Size * 4];
        for (var d = 0; d < digits.Length; d++)
        {
            var left = ((Size - width) / 2) + (d * (glyphWidth + Gap));
            DrawGlyph(pixels, font[digits[d] - '0'], left, (Size - GlyphHeight) / 2, ink);
        }

        return PngWriter.Encode(pixels, Size, Size);
    }

    private static byte[] ToRgba(uint rgb) => [(byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xFF];

    private static void DrawGlyph(byte[] pixels, string glyph, int left, int top, byte[] ink)
    {
        var rows = glyph.Split(' ');
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x] == '#')
                {
                    ink.CopyTo(pixels, (((top + y) * Size) + left + x) * 4);
                }
            }
        }
    }
}
