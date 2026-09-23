using System.Globalization;
using System.Resources;

namespace Hekutenantcoreapp.Application.Resources;

// Reads LogMessages.resx directly via ResourceManager with CultureInfo.InvariantCulture pinned —
// deliberately NOT IStringLocalizer<LogMessages> (the pattern Messages.resx uses), because
// IStringLocalizer resolves against the ambient per-request CurrentUICulture. Log text must not
// vary by which tenant/user triggered the action, or the log stream becomes an unsearchable mix
// of languages. English-only today; a LogMessages.es.resx could be added later without changing
// this resolution behavior.
public static class LogText
{
    private static readonly ResourceManager ResourceManager =
        new("Hekutenantcoreapp.Application.Resources.LogMessages", typeof(LogText).Assembly);

    public static string Get(string key, params object[] args)
    {
        var template = ResourceManager.GetString(key, CultureInfo.InvariantCulture) ?? key;
        return args.Length == 0 ? template : string.Format(CultureInfo.InvariantCulture, template, args);
    }
}
