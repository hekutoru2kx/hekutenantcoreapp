namespace Hekutenantcoreapp.Application.DTOs;

public class UpsertEmployeeDto
{
    public string? JobTitle { get; set; }
    public DateTime? HireDate { get; set; }
    public bool IsActive { get; set; } = true;
}
