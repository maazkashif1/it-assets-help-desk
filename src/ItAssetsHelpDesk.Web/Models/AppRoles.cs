namespace ItAssetsHelpDesk.Web.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string ITStaff = "ITStaff";
    public const string Employee = "Employee";

    public static readonly string[] All = { Admin, ITStaff, Employee };
}
