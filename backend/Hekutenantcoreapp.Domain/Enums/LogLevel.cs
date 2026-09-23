namespace Hekutenantcoreapp.Domain.Enums;

// Mirrors Microsoft.Extensions.Logging.LogLevel's names and numeric values exactly (Domain stays
// dependency-free, so it can't reference that package directly) — Infrastructure/Api convert
// between the two with a plain (Microsoft.Extensions.Logging.LogLevel)(int)value cast, no mapping
// table needed. None means "this category is fully disabled".
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}
