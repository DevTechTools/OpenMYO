using OpenMyo.Core.Icons;

namespace OpenMyo.Core.Tests.Fakes;

public static class TestIcons
{
    public static DisplayIcon Make(string mediaId, string title, params string[] tags) =>
        new(mediaId, title, tags, null, IsPublic: true);
}
