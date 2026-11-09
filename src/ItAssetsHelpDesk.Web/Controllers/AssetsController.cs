using ItAssetsHelpDesk.Web.Data;
using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ItAssetsHelpDesk.Web.Controllers;

[Authorize]
public class AssetsController : Controller
{
    private const string ManagerRoles = $"{AppRoles.Admin},{AppRoles.ITStaff}";

    private readonly ApplicationDbContext _db;

    public AssetsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Table data is loaded client-side from /api/assets so the search and
    // filter controls can update it live without a round trip here.
    public async Task<IActionResult> Index()
    {
        ViewBag.Departments = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        return View();
    }

    public async Task<IActionResult> Details(int id)
    {
        var asset = await _db.Assets
            .Include(a => a.Department)
            .Include(a => a.AssignedToUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asset is null) return NotFound();

        return View(asset);
    }

    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Create()
    {
        return View(await BuildFormViewModel(new AssetFormViewModel()));
    }

    [HttpPost]
    [Authorize(Roles = ManagerRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(await BuildFormViewModel(vm));

        if (await _db.Assets.AnyAsync(a => a.AssetTag == vm.AssetTag))
        {
            ModelState.AddModelError(nameof(vm.AssetTag), "An asset with this tag already exists.");
            return View(await BuildFormViewModel(vm));
        }

        var asset = new Asset();
        ApplyForm(asset, vm);
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Asset {asset.AssetTag} created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Edit(int id)
    {
        var asset = await _db.Assets.FindAsync(id);
        if (asset is null) return NotFound();

        var vm = await BuildFormViewModel(new AssetFormViewModel
        {
            Id = asset.Id,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            Type = asset.Type,
            Status = asset.Status,
            SerialNumber = asset.SerialNumber,
            SeatsTotal = asset.SeatsTotal,
            SeatsUsed = asset.SeatsUsed,
            PurchaseCost = asset.PurchaseCost,
            PurchaseDate = asset.PurchaseDate,
            ExpiryDate = asset.ExpiryDate,
            DepartmentId = asset.DepartmentId,
            AssignedToUserId = asset.AssignedToUserId
        });

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = ManagerRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssetFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        var asset = await _db.Assets.FindAsync(id);
        if (asset is null) return NotFound();

        if (!ModelState.IsValid) return View(await BuildFormViewModel(vm));

        if (await _db.Assets.AnyAsync(a => a.AssetTag == vm.AssetTag && a.Id != id))
        {
            ModelState.AddModelError(nameof(vm.AssetTag), "An asset with this tag already exists.");
            return View(await BuildFormViewModel(vm));
        }

        ApplyForm(asset, vm);
        asset.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Asset {asset.AssetTag} updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = ManagerRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var asset = await _db.Assets.FindAsync(id);
        if (asset is null) return NotFound();

        _db.Assets.Remove(asset); // intercepted as a soft delete in ApplicationDbContext
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Asset {asset.AssetTag} deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static void ApplyForm(Asset asset, AssetFormViewModel vm)
    {
        asset.AssetTag = vm.AssetTag.Trim();
        asset.Name = vm.Name.Trim();
        asset.Type = vm.Type;
        asset.Status = vm.Status;
        asset.SerialNumber = vm.SerialNumber;
        asset.SeatsTotal = vm.SeatsTotal;
        asset.SeatsUsed = vm.SeatsUsed;
        asset.PurchaseCost = vm.PurchaseCost;
        asset.PurchaseDate = vm.PurchaseDate;
        asset.ExpiryDate = vm.ExpiryDate;
        asset.DepartmentId = vm.DepartmentId;
        asset.AssignedToUserId = string.IsNullOrEmpty(vm.AssignedToUserId) ? null : vm.AssignedToUserId;
    }

    private async Task<AssetFormViewModel> BuildFormViewModel(AssetFormViewModel vm)
    {
        var departments = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();

        vm.DepartmentOptions = departments
            .Select(d => new SelectListItem(d.Name, d.Id.ToString()))
            .ToList();

        vm.UserOptions = users
            .Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id))
            .ToList();

        return vm;
    }
}
