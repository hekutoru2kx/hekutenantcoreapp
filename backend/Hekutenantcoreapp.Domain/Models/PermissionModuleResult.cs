namespace Hekutenantcoreapp.Domain.Models;

public class PermissionModuleResult
{
    public string Module { get; set; } = string.Empty;
    public IList<string> Actions { get; set; } = new List<string>();
}
