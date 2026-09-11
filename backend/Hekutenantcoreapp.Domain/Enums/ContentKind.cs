namespace Hekutenantcoreapp.Domain.Enums;

// What a ContentItem row carries. Explicit int values (persisted); parsed by name at the
// repository boundary — same convention as every other enum here. Image/File are the only
// kinds wired to an upload endpoint so far (Person.ProfilePicture); Text/Link/Video/Document
// exist on the schema for future consumers but have no sanitization/validation pipeline yet —
// that lands with the first consumer that needs it. Ported from hekucoreapp 2026-09-11.
public enum ContentKind
{
    // Body holds server-sanitized authored HTML; Url and StoredFileId null. Not wired yet.
    Text = 1,

    // Url is an external hyperlink. Not wired yet.
    Link = 2,

    // Url is an external video page (YouTube/Vimeo), rendered as an embed. Not wired yet.
    Video = 3,

    // Url points at an external document (PDF/slides/Drive). Not wired yet.
    Document = 4,

    // An image. Exactly one of Url (external image) or StoredFileId (uploaded) is set.
    Image = 5,

    // An arbitrary uploaded file. StoredFileId set, Url null.
    File = 6
}
