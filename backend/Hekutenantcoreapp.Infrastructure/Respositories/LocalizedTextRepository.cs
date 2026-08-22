using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

public class LocalizedTextRepository : ILocalizedTextRepository
{
    private readonly HekutenantcoreappDbContext _context;

    public LocalizedTextRepository(HekutenantcoreappDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, Dictionary<string, string>>> GetTranslationsAsync(string entityName, IList<int> entityIds, string fieldName)
    {
        var rows = await _context.LocalizedTexts
            .Where(l => l.EntityName == entityName && l.FieldName == fieldName && entityIds.Contains(l.EntityId))
            .ToListAsync();

        return rows
            .GroupBy(r => r.EntityId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.LanguageCode, r => r.Value));
    }

    public async Task ReplaceTranslationsAsync(string entityName, int entityId, string fieldName, IDictionary<string, string> translations)
    {
        var existing = await _context.LocalizedTexts
            .Where(l => l.EntityName == entityName && l.EntityId == entityId && l.FieldName == fieldName)
            .ToListAsync();

        var toRemove = existing.Where(e => !translations.ContainsKey(e.LanguageCode)).ToList();
        if (toRemove.Count > 0)
            _context.LocalizedTexts.RemoveRange(toRemove);

        foreach (var (languageCode, value) in translations)
        {
            var existingRow = existing.FirstOrDefault(e => e.LanguageCode == languageCode);
            if (existingRow != null)
            {
                existingRow.Value = value;
            }
            else
            {
                _context.LocalizedTexts.Add(new LocalizedText
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    FieldName = fieldName,
                    LanguageCode = languageCode,
                    Value = value
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}
