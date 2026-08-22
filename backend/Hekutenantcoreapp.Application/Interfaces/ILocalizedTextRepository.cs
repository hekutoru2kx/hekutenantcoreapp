namespace Hekutenantcoreapp.Application.Interfaces;

public interface ILocalizedTextRepository
{
    Task<Dictionary<int, Dictionary<string, string>>> GetTranslationsAsync(string entityName, IList<int> entityIds, string fieldName);
    Task ReplaceTranslationsAsync(string entityName, int entityId, string fieldName, IDictionary<string, string> translations);
}
