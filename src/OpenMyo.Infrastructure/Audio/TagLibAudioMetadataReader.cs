using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Abstractions;
using TagLib;

namespace OpenMyo.Infrastructure.Audio;

/// <summary>Reads audio tags with TagLib#. Stateless, so safe to call from many threads.</summary>
public sealed partial class TagLibAudioMetadataReader(ILogger<TagLibAudioMetadataReader> logger) : IAudioMetadataReader
{
    private const string DefaultPictureMimeType = "image/jpeg";

    public AudioMetadata Read(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        try
        {
            using var file = TagLib.File.Create(filePath, ReadStyle.Average);
            var tag = file.Tag;
            return new AudioMetadata(
                NullIfEmpty(tag.Title),
                NullIfEmpty(tag.Album),
                NullIfZero(tag.Track),
                NullIfZero(tag.Disc),
                file.Properties?.Duration ?? TimeSpan.Zero,
                HashOf(PickPicture(tag)));
        }
        catch (Exception ex) when (IsUnreadable(ex))
        {
            LogUnreadable(ex, filePath);
            return AudioMetadata.Empty;
        }
    }

    public EmbeddedPicture? ReadPicture(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        try
        {
            using var file = TagLib.File.Create(filePath, ReadStyle.Average);
            var picture = PickPicture(file.Tag);
            if (picture is null)
            {
                return null;
            }

            var mimeType = string.IsNullOrWhiteSpace(picture.MimeType) ? DefaultPictureMimeType : picture.MimeType;
            return new EmbeddedPicture(picture.Data.Data, mimeType);
        }
        catch (Exception ex) when (IsUnreadable(ex))
        {
            LogUnreadable(ex, filePath);
            return null;
        }
    }

    private static bool IsUnreadable(Exception ex) =>
        ex is CorruptFileException or UnsupportedFormatException or IOException or UnauthorizedAccessException;

    private static IPicture? PickPicture(Tag tag)
    {
        var pictures = tag.Pictures;
        foreach (var picture in pictures)
        {
            if (picture.Type == PictureType.FrontCover && picture.Data.Count > 0)
            {
                return picture;
            }
        }

        return pictures.FirstOrDefault(picture => picture.Data.Count > 0);
    }

    private static string? HashOf(IPicture? picture) =>
        picture is null ? null : Convert.ToHexStringLower(SHA256.HashData(picture.Data.Data));

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static uint? NullIfZero(uint value) => value == 0 ? null : value;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Could not read audio tags from {FilePath}.")]
    private partial void LogUnreadable(Exception exception, string filePath);
}
