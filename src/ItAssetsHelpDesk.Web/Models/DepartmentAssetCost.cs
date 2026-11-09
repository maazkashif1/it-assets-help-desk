namespace ItAssetsHelpDesk.Web.Models;

// Keyless DTO populated from the dbo.GetAssetCostByDepartment stored procedure.
public class DepartmentAssetCost
{
    public string DepartmentName { get; set; } = string.Empty;
    public int AssetCount { get; set; }
    public decimal TotalCost { get; set; }
}
