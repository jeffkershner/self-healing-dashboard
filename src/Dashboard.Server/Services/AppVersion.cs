using System.Reflection;
using Dashboard.Shared;

namespace Dashboard.Server.Services;

/// <summary>Reads the version stamped at build time (-p:Version) plus the git SHA the SDK appends.</summary>
public static class AppVersion
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    public static string Informational { get; } =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    public static string Version { get; } = Informational.Split('+')[0];

    public static string Commit { get; } = ExtractCommit(Informational);

    public static VersionInfo Describe(string environment) => new(Version, Commit, environment, StartedAt);

    private static string ExtractCommit(string informational)
    {
        var plus = informational.IndexOf('+');
        if (plus < 0 || plus == informational.Length - 1)
        {
            return "local";
        }

        var sha = informational[(plus + 1)..];
        return sha.Length > 7 ? sha[..7] : sha;
    }
}
