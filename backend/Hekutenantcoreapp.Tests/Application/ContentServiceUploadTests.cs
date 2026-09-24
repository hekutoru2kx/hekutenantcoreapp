using System.Text;
using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Application.Services;
using Hekutenantcoreapp.Domain.Constants;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hekutenantcoreapp.Tests.Application;

// Covers UploadToSlotAsync's file-type gate. The original feature only smoke-tested the happy
// path, which let a wrong-type upload leak ImageSharp's raw UnknownImageFormatException text
// ("Image cannot be loaded. Available decoders: ...") out as the 400 body instead of the
// localized ContentFileTypeNotAllowed message.
public class ContentServiceUploadTests
{
    private const string NotAllowedKey = "ContentFileTypeNotAllowed";

    private readonly Mock<IContentRepository> _repository = new();
    private readonly Mock<IContentStorage> _storage = new();
    private readonly Mock<IAppSettingsService> _appSettings = new();

    private ContentService CreateService(string allowedTypes = "image/png,image/jpeg")
    {
        _appSettings.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new AppSettingsResult
        {
            ContentMaxBytes = 1024 * 1024,
            ContentAllowedContentTypes = allowedTypes,
            ContentMaxImageDimension = 2048,
            ContentAvatarMaxDimension = 512
        });
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredBlobRef("test", "blob"));
        _repository.Setup(r => r.UpsertFileSlotAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<StoredFile>(), It.IsAny<ContentKind>(), It.IsAny<string>()))
            .ReturnsAsync((string ownerType, int ownerId, string slot, StoredFile file, ContentKind kind, string title) =>
                (new ContentItem { OwnerType = ownerType, OwnerId = ownerId, Slot = slot, Kind = kind, Title = title, StoredFile = file }, (StoredFile?)null));

        var localizer = new Mock<IStringLocalizer<Messages>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        return new ContentService(_repository.Object, _storage.Object, _appSettings.Object, new Mock<IConfiguration>().Object, localizer.Object);
    }

    private static MemoryStream PngStream()
    {
        using var image = new Image<Rgba32>(4, 4);
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    private Task<ContentItemResult> Upload(ContentService service, Stream stream, string fileName) =>
        service.UploadToSlotAsync(ContentOwnerTypes.Person, 1, ContentSlots.ProfilePicture, stream, fileName, "image/png");

    [Fact]
    public async Task PlainTextUpload_IsRejectedWithLocalizedMessage()
    {
        var service = CreateService();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("definitely not an image"));

        var ex = await Assert.ThrowsAsync<Exception>(() => Upload(service, stream, "notes.png"));

        Assert.Equal(NotAllowedKey, ex.Message);
        _storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PdfUpload_IsRejectedWithLocalizedMessage()
    {
        var service = CreateService();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\n%âã\n1 0 obj\n<<>>\nendobj\n"));

        var ex = await Assert.ThrowsAsync<Exception>(() => Upload(service, stream, "doc.pdf"));

        Assert.Equal(NotAllowedKey, ex.Message);
    }

    [Fact]
    public async Task ValidImageOfDisallowedType_IsRejectedWithLocalizedMessage()
    {
        var service = CreateService(allowedTypes: "image/jpeg");
        using var stream = PngStream();

        var ex = await Assert.ThrowsAsync<Exception>(() => Upload(service, stream, "avatar.png"));

        Assert.Equal(NotAllowedKey, ex.Message);
    }

    [Fact]
    public async Task ValidAllowedImage_IsStoredWithSniffedContentType()
    {
        var service = CreateService();
        using var stream = PngStream();

        await Upload(service, stream, "avatar.png");

        _storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), "avatar.png", "image/png", It.IsAny<CancellationToken>()), Times.Once);
    }
}
