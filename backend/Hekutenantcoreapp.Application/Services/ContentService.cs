using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Constants;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;

namespace Hekutenantcoreapp.Application.Services;

// Ported verbatim from hekucoreapp 2026-09-11 (namespace only) — tenancy is entirely invisible
// here. ContentItem/StoredFile being ITenantScoped means every query/insert this class makes
// through IContentRepository is automatically tenant-filtered/tenant-stamped by the DbContext;
// this class never touches TenantId.
public class ContentService : IContentService
{
    // Absolute ceiling a DB-configured AppSettings.ContentMaxBytes can't exceed — restart-only
    // by nature (appsettings.json), unlike the DB-backed limits below it.
    private const long DefaultHardMaxBytes = 10 * 1024 * 1024;

    private readonly IContentRepository _repository;
    private readonly IContentStorage _storage;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<Messages> _localizer;

    public ContentService(
        IContentRepository repository,
        IContentStorage storage,
        IAppSettingsService appSettingsService,
        IConfiguration configuration,
        IStringLocalizer<Messages> localizer)
    {
        _repository = repository;
        _storage = storage;
        _appSettingsService = appSettingsService;
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<ContentItemResult?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToResult(item);
    }

    public async Task<ContentItemResult?> GetSlotAsync(string ownerType, int ownerId, string slot)
    {
        EnsureValidOwnerType(ownerType);
        var item = await _repository.GetSlotAsync(ownerType, ownerId, slot);
        return item == null ? null : MapToResult(item);
    }

    public async Task<IReadOnlyList<ContentItemResult>> GetForOwnerAsync(string ownerType, int ownerId, bool includeUnpublished = false, bool includeArchived = false)
    {
        EnsureValidOwnerType(ownerType);
        var items = await _repository.GetForOwnerAsync(ownerType, ownerId, includeUnpublished, includeArchived);
        return items.Select(MapToResult).ToList();
    }

    public async Task<ContentItemResult> UploadToSlotAsync(string ownerType, int ownerId, string slot, Stream fileStream, string fileName, string? declaredContentType)
    {
        EnsureValidOwnerType(ownerType);

        var settings = await _appSettingsService.GetSettingsAsync();
        var hardMaxBytes = long.TryParse(_configuration["ContentStorage:HardMaxBytes"], out var configuredHardMax)
            ? configuredHardMax
            : DefaultHardMaxBytes;
        var maxBytes = Math.Min(settings.ContentMaxBytes > 0 ? settings.ContentMaxBytes : hardMaxBytes, hardMaxBytes);
        var allowedTypes = (settings.ContentAllowedContentTypes ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Buffer fully before trusting anything about it — size, and the *sniffed* format
        // rather than the client-declared content type.
        await using var buffered = new MemoryStream();
        await fileStream.CopyToAsync(buffered);
        if (buffered.Length == 0)
            throw new Exception(_localizer["ContentFileEmpty"]);
        if (buffered.Length > maxBytes)
            throw new Exception(_localizer["ContentFileTooLarge"]);

        buffered.Position = 0;
        var detectedFormat = await Image.DetectFormatAsync(buffered);
        if (detectedFormat == null || !allowedTypes.Contains(detectedFormat.DefaultMimeType, StringComparer.OrdinalIgnoreCase))
            throw new Exception(_localizer["ContentFileTypeNotAllowed"]);
        var contentType = detectedFormat.DefaultMimeType;

        buffered.Position = 0;
        using var image = await Image.LoadAsync(buffered);

        var maxDimension = slot == ContentSlots.ProfilePicture
            ? settings.ContentAvatarMaxDimension
            : settings.ContentMaxImageDimension;
        if (maxDimension > 0 && (image.Width > maxDimension || image.Height > maxDimension))
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxDimension, maxDimension)
            }));
        }

        // Re-encoding (rather than storing the upload verbatim) is what strips EXIF/metadata
        // and bounds the stored byte size to the resized dimensions.
        await using var processed = new MemoryStream();
        await image.SaveAsync(processed, detectedFormat);
        var byteSize = processed.Length;

        processed.Position = 0;
        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(processed));

        processed.Position = 0;
        var blobRef = await _storage.SaveAsync(processed, fileName, contentType);

        var storedFile = new StoredFile
        {
            Container = blobRef.Container,
            BlobName = blobRef.BlobName,
            OriginalFileName = fileName,
            ContentType = contentType,
            ByteSize = byteSize,
            Sha256 = sha256,
            Width = image.Width,
            Height = image.Height
        };

        var (savedItem, replacedFile) = await _repository.UpsertFileSlotAsync(ownerType, ownerId, slot, storedFile, ContentKind.Image, title: slot);

        if (replacedFile != null)
            await _storage.DeleteAsync(replacedFile.Container, replacedFile.BlobName);

        return MapToResult(savedItem);
    }

    public async Task DeleteSlotAsync(string ownerType, int ownerId, string slot)
    {
        EnsureValidOwnerType(ownerType);
        var removedFile = await _repository.DeleteSlotAsync(ownerType, ownerId, slot);
        if (removedFile != null)
            await _storage.DeleteAsync(removedFile.Container, removedFile.BlobName);
    }

    public async Task<(Stream Stream, string ContentType, string FileName, string ETag)> OpenFileAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id)
            ?? throw new Exception(_localizer["ContentNotFound"]);
        if (item.StoredFile == null)
            throw new Exception(_localizer["ContentHasNoFile"]);

        var stream = await _storage.OpenReadAsync(item.StoredFile.Container, item.StoredFile.BlobName);
        return (stream, item.StoredFile.ContentType, item.StoredFile.OriginalFileName, item.StoredFile.Sha256);
    }

    private static void EnsureValidOwnerType(string ownerType)
    {
        if (!ContentOwnerTypes.IsValid(ownerType))
            throw new ArgumentException($"Unknown content owner type '{ownerType}'.", nameof(ownerType));
    }

    private static ContentItemResult MapToResult(ContentItem item) => new()
    {
        Id = item.Id,
        OwnerType = item.OwnerType,
        OwnerId = item.OwnerId,
        Slot = item.Slot,
        Kind = item.Kind.ToString(),
        Title = item.Title,
        Description = item.Description,
        Url = item.Url,
        HasFile = item.StoredFileId != null,
        DisplayOrder = item.DisplayOrder,
        PublishedAt = item.PublishedAt,
        ArchivedAt = item.ArchivedAt
    };
}
