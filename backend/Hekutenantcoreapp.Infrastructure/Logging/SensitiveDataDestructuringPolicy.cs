using Serilog.Core;
using Serilog.Events;
using System.Reflection;

namespace Hekutenantcoreapp.Infrastructure.Logging;

// Safety net behind "the Http category never logs request bodies": if any code ever destructures
// a whole object into a log event (Serilog's `{@x}` syntax), this masks any property whose name
// matches a known-sensitive list, on ANY type, present or future — not a per-DTO opt-in, since a
// per-DTO rule is exactly the kind of thing a new field slips past.
public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "PasswordHash", "ConfirmPassword", "CurrentPassword", "NewPassword",
        "Secret", "ClientSecret", "Token", "ConnectionString", "Key"
    };

    private const string RedactedValue = "***REDACTED***";

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = null!;
        var type = value.GetType();

        if (type.IsPrimitive || type == typeof(string) || (type.Namespace?.StartsWith("System") ?? false))
            return false;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToList();

        if (!properties.Any(p => SensitivePropertyNames.Contains(p.Name)))
            return false; // nothing sensitive on this type — defer to Serilog's default policy

        var structureProperties = new List<LogEventProperty>();
        foreach (var prop in properties)
        {
            object? rawValue;
            try { rawValue = prop.GetValue(value); }
            catch { continue; }

            var propertyValue = SensitivePropertyNames.Contains(prop.Name)
                ? new ScalarValue(RedactedValue)
                : propertyValueFactory.CreatePropertyValue(rawValue, destructureObjects: true);

            structureProperties.Add(new LogEventProperty(prop.Name, propertyValue));
        }

        result = new StructureValue(structureProperties, type.Name);
        return true;
    }
}
