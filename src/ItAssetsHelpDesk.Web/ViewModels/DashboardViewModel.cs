using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.Models.Enums;

namespace ItAssetsHelpDesk.Web.ViewModels;

public class DashboardViewModel
{
    public List<PriorityCount> OpenTicketsByPriority { get; set; } = new();
    public List<Asset> ExpiringSoon { get; set; } = new();
    public bool ShowFinancials { get; set; }
    public List<DepartmentCount> AssetsPerDepartment { get; set; } = new();
    public List<DepartmentAssetCost> CostByDepartment { get; set; } = new();
}

public class PriorityCount
{
    public TicketPriority Priority { get; set; }
    public int Count { get; set; }
}

public class DepartmentCount
{
    public string DepartmentName { get; set; } = string.Empty;
    public int Count { get; set; }
}
