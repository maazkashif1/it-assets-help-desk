using ItAssetsHelpDesk.Web.Data;
using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.Models.Enums;
using ItAssetsHelpDesk.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItAssetsHelpDesk.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var cutoff = DateTime.UtcNow.AddDays(30);

        var vm = new DashboardViewModel
        {
            OpenTicketsByPriority = await _db.Tickets
                .Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                .GroupBy(t => t.Priority)
                .Select(g => new PriorityCount { Priority = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Priority)
                .ToListAsync(),

            ExpiringSoon = await _db.Assets
                .Include(a => a.Department)
                .Where(a => a.ExpiryDate != null && a.ExpiryDate <= cutoff)
                .OrderBy(a => a.ExpiryDate)
                .ToListAsync(),

            ShowFinancials = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITStaff)
        };

        if (vm.ShowFinancials)
        {
            vm.AssetsPerDepartment = await _db.Assets
                .Include(a => a.Department)
                .GroupBy(a => a.Department!.Name)
                .Select(g => new DepartmentCount { DepartmentName = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            // Hand-written stored procedure: dbo.GetAssetCostByDepartment
            vm.CostByDepartment = await _db.DepartmentAssetCosts
                .FromSqlRaw("EXEC dbo.GetAssetCostByDepartment")
                .ToListAsync();
        }

        return View(vm);
    }
}
