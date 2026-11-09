using Microsoft.AspNetCore.Identity;

namespace ItAssetsHelpDesk.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
}
