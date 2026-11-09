using System.Text.Json;
using ItAssetsHelpDesk.Web.Data;
using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ItAssetsHelpDesk.Web.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Table data is loaded client-side from /api/tickets (filtered to "my
    // tickets" for employees) so search/status/priority filters update live.
    public async Task<IActionResult> Index()
    {
        var itStaff = await _userManager.GetUsersInRoleAsync(AppRoles.ITStaff);
        ViewBag.ItStaffJson = JsonSerializer.Serialize(
            itStaff.OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName }));
        ViewBag.IsManager = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITStaff);
        return View();
    }

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Department)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Include(t => t.RelatedAsset)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var isManager = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITStaff);
        if (!isManager && ticket.CreatedByUserId != currentUserId) return Forbid();

        ViewBag.IsManager = isManager;
        if (isManager)
        {
            var itStaff = await _userManager.GetUsersInRoleAsync(AppRoles.ITStaff);
            ViewBag.ItStaffOptions = itStaff.OrderBy(u => u.FullName)
                .Select(u => new SelectListItem(u.FullName, u.Id))
                .ToList();
        }

        return View(ticket);
    }

    public async Task<IActionResult> Create()
    {
        return View(await BuildFormViewModel(new TicketFormViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(await BuildFormViewModel(vm));

        var user = await _userManager.GetUserAsync(User);
        if (user?.DepartmentId is null)
        {
            ModelState.AddModelError(string.Empty, "Your account has no department assigned; contact an administrator.");
            return View(await BuildFormViewModel(vm));
        }

        var ticket = new Ticket
        {
            Title = vm.Title.Trim(),
            Description = vm.Description.Trim(),
            Priority = vm.Priority,
            RelatedAssetId = vm.RelatedAssetId,
            CreatedByUserId = user.Id,
            DepartmentId = user.DepartmentId.Value
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Ticket submitted.";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    private async Task<TicketFormViewModel> BuildFormViewModel(TicketFormViewModel vm)
    {
        var assets = await _db.Assets.OrderBy(a => a.AssetTag).ToListAsync();
        vm.AssetOptions = assets
            .Select(a => new SelectListItem($"{a.AssetTag} - {a.Name}", a.Id.ToString()))
            .ToList();
        return vm;
    }
}
