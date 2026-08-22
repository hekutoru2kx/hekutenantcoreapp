namespace Hekutenantcoreapp.Domain.Models;

public class PersonMatchResult
{
    // No matching person found by document — nothing to report.
    public bool MatchFound { get; set; }

    // A match was found and the given email agrees with it — submitting will link to it.
    // False with MatchFound true means a document match exists but can't be linked
    // (already linked to someone else, or the email doesn't agree) — mirrors the same
    // check UpsertPersonAsync performs at submit time.
    public bool Linkable { get; set; }
}
