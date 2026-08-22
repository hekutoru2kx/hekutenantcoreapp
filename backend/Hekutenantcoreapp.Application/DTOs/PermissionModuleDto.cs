namespace Hekutenantcoreapp.Application.DTOs;

public class PermissionModuleDto
{
    public string Module { get; set; } = string.Empty;
    public IList<string> Actions { get; set; } = new List<string>();
}
