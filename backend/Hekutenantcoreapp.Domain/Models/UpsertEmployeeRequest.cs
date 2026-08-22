namespace Hekutenantcoreapp.Domain.Models;

public class UpsertEmployeeRequest
{
    public string? JobTitle { get; set; }
    public DateTime? HireDate { get; set; }
    public bool IsActive { get; set; } = true;
}
