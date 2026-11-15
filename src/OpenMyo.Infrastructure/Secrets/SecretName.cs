using System.Text.RegularExpressions;

namespace OpenMyo.Infrastructure.Secrets;

/// <summary>Secret names double as keychain accounts and file names, so only a safe alphabet is allowed.</summary>
internal static partial class SecretName
{
    public static string Validate(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Allowed().IsMatch(name))
        {
            throw new ArgumentException("Secret names may only contain lowercase letters, digits and hyphens.", nameof(name));
        }

        return name;
    }

    [GeneratedRegex("^[a-z0-9-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Allowed();
}
