namespace Hekutenantcoreapp.Application.DTOs;

public class AppSettingsDto
{
    public bool RequireEmailConfirmation { get; set; }
    public long ContentMaxBytes { get; set; }
    public string ContentAllowedContentTypes { get; set; } = string.Empty;
    public int ContentMaxImageDimension { get; set; }
    public int ContentAvatarMaxDimension { get; set; }
}
