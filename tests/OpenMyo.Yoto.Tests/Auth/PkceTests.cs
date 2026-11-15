using OpenMyo.Yoto.Auth;

namespace OpenMyo.Yoto.Tests.Auth;

public sealed class PkceTests
{
    [Fact]
    public void CreateChallenge_Rfc7636AppendixB_MatchesExpected()
    {
        var challenge = Pkce.CreateChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk");

        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", challenge);
    }

    [Fact]
    public void CreateVerifier_Always_Has64UrlSafeCharacters()
    {
        var verifier = Pkce.CreateVerifier();

        Assert.Equal(64, verifier.Length);
        Assert.All(verifier, c => Assert.True(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'));
    }

    [Fact]
    public void CreateState_CalledTwice_ReturnsDifferentValues() =>
        Assert.NotEqual(Pkce.CreateState(), Pkce.CreateState());

    [Fact]
    public void BuildAuthorizeUri_Always_IncludesPkceAndState()
    {
        var uri = YotoAuthEndpoints.BuildAuthorizeUri("client1", "chal", "st8");

        Assert.Contains("code_challenge=chal", uri.Query, StringComparison.Ordinal);
        Assert.Contains("code_challenge_method=S256", uri.Query, StringComparison.Ordinal);
        Assert.Contains("state=st8", uri.Query, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=http%3A%2F%2F127.0.0.1%3A8787%2Fcallback", uri.Query, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/callback?code=abc&state=s", "abc", null)]
    [InlineData("/callback?code=abc&state=other", null, "state_mismatch")]
    [InlineData("/callback?error=access_denied", null, "access_denied")]
    public void Parse_CallbackQuery_ReturnsOutcome(string pathAndQuery, string? code, string? error)
    {
        var result = AuthorizationCallback.Parse(new Uri("http://127.0.0.1:8787" + pathAndQuery), "s");

        Assert.Equal(code, result?.Code);
        Assert.Equal(error, result?.Error);
    }

    [Fact]
    public void Parse_OtherPath_ReturnsNull() =>
        Assert.Null(AuthorizationCallback.Parse(new Uri("http://127.0.0.1:8787/favicon.ico"), "s"));
}
