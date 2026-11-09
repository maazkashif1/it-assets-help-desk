using ItAssetsHelpDesk.Web.Data;
using ItAssetsHelpDesk.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItAssetsHelpDesk.Web.Controllers.Api;

// Small REST API used by the Assets page's live filter UI, and usable
// standalone (e.g. from curl/Postman) for the asset register.
[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsApiController : ControllerBase
{
    private const string ManagerRoles = $"{AppRoles.Admin},{AppRoles.ITStaff}";

    private readonly ApplicationDbContext _db;

    public AssetsApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Models.Enums.AssetType? type,
        [FromQuery] int? departmentId)
    {
        var query = _db.Assets.Include(a => a.Department).Include(a => a.AssignedToUser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.AssetTag.Contains(term) || a.Name.Contains(term));
        }

        if (type.HasValue) query = query.Where(a => a.Type == type);
        if (departmentId.HasValue) query = query.Where(a => a.DepartmentId == departmentId);

        var assets = await query.OrderBy(a => a.AssetTag).Select(a => ToDto(a)).ToListAsync();
        return Ok(assets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetDto>> GetById(int id)
    {
        var asset = await _db.Assets.Include(a => a.Department).Include(a => a.AssignedToUser)
            .FirstOrDefaultAsync(a => a.Id == id);
        return asset is null ? NotFound() : Ok(ToDto(asset));
    }

    [HttpPost]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<AssetDto>> Create(AssetRequest request)
    {
        if (await _db.Assets.AnyAsync(a => a.AssetTag == request.AssetTag))
        {
            return Conflict($"Asset tag '{request.AssetTag}' already exists.");
        }

        var asset = new Asset();
        Apply(asset, request);
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        await _db.Entry(asset).Reference(a => a.Department).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, ToDto(asset));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Update(int id, AssetRequest request)
    {
        var asset = await _db.Assets.FindAsync(id);
        if (asset is null) return NotFound();

        if (await _db.Assets.AnyAsync(a => a.AssetTag == request.AssetTag && a.Id != id))
        {
            return Conflict($"Asset tag '{request.AssetTag}' already exists.");
        }

        Apply(asset, request);
        asset.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Delete(int id)
    {
        var asset = await _db.Assets.FindAsync(id);
        if (asset is null) return NotFound();

        _db.Assets.Remove(asset); // soft-deleted via ApplicationDbContext
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Asset asset, AssetRequest request)
    {
        asset.AssetTag = request.AssetTag.Trim();
        asset.Name = request.Name.Trim();
        asset.Type = request.Type;
        asset.Status = request.Status;
        asset.SerialNumber = request.SerialNumber;
        asset.SeatsTotal = request.SeatsTotal;
        asset.SeatsUsed = request.SeatsUsed;
        asset.PurchaseCost = request.PurchaseCost;
        asset.PurchaseDate = request.PurchaseDate;
        asset.ExpiryDate = request.ExpiryDate;
        asset.DepartmentId = request.DepartmentId;
        asset.AssignedToUserId = string.IsNullOrEmpty(request.AssignedToUserId) ? null : request.AssignedToUserId;
    }

    private static AssetDto ToDto(Asset a) => new(
        a.Id, a.AssetTag, a.Name, a.Type, a.Status, a.SerialNumber, a.SeatsTotal, a.SeatsUsed,
        a.PurchaseCost, a.PurchaseDate, a.ExpiryDate, a.DepartmentId, a.Department?.Name,
        a.AssignedToUserId, a.AssignedToUser?.FullName);
}
