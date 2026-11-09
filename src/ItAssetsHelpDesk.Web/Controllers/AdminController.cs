using ItAssetsHelpDesk.Web.Data;
using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItAssetsHelpDesk.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> AuditLog(string? entityName, AuditAction? action, int page = 1)
    {
        const int pageSize = 50;

        var query = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName == entityName);
        if (action.HasValue) query = query.Where(a => a.Action == action);

        ViewBag.EntityNames = await _db.AuditLogs.Select(a => a.EntityName).Distinct().OrderBy(n => n).ToListAsync();
        ViewBag.SelectedEntityName = entityName;
        ViewBag.SelectedAction = action;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(logs);
    }
}
