namespace Hekutenantcoreapp.Domain.Common;

public interface ITenantScoped
{
    int TenantId { get; set; }
}
