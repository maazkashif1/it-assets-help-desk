using ItAssetsHelpDesk.Web.Data;
using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItAssetsHelpDesk.Web.Controllers.Api;

// Small REST API used by the Tickets page's live search/filter UI and the
// inline status/assignment controls (no full page reload needed).
[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsApiController : ControllerBase
{
    private const string ManagerRoles = $"{AppRoles.Admin},{AppRoles.ITStaff}";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketsApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority)
    {
        var query = _db.Tickets
            .Include(t => t.Department)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Include(t => t.RelatedAsset)
            .AsQueryable();

        var isManager = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITStaff);
        if (!isManager)
        {
            var userId = _userManager.GetUserId(User);
            query = query.Where(t => t.CreatedByUserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.Title.Contains(term) || t.Description.Contains(term));
        }

        if (status.HasValue) query = query.Where(t => t.Status == status);
        if (priority.HasValue) query = query.Where(t => t.Priority == priority);

        var tickets = await query.OrderByDescending(t => t.CreatedAt).Select(t => ToDto(t)).ToListAsync();
        return Ok(tickets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDto>> GetById(int id)
    {
        var ticket = await Load(id);
        if (ticket is null) return NotFound();
        if (!CanView(ticket)) return Forbid();
        return Ok(ToDto(ticket));
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create(TicketCreateRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.DepartmentId is null) return BadRequest("Your account has no department assigned.");

        var ticket = new Ticket
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority,
            RelatedAssetId = request.RelatedAssetId,
            CreatedByUserId = user.Id,
            DepartmentId = user.DepartmentId.Value
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var created = await Load(ticket.Id);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ToDto(created!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Update(int id, TicketUpdateRequest request)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket is null) return NotFound();

        if (request.Status.HasValue)
        {
            ticket.Status = request.Status.Value;
            ticket.ClosedAt = request.Status.Value == TicketStatus.Closed ? DateTime.UtcNow : null;
        }

        if (request.Priority.HasValue) ticket.Priority = request.Priority.Value;

        if (request.AssignedToUserId is not null)
        {
            ticket.AssignedToUserId = request.AssignedToUserId.Length == 0 ? null : request.AssignedToUserId;
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket is null) return NotFound();

        _db.Tickets.Remove(ticket); // soft-deleted via ApplicationDbContext
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private bool CanView(Ticket ticket) =>
        User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.ITStaff) ||
        ticket.CreatedByUserId == _userManager.GetUserId(User);

    private Task<Ticket?> Load(int id) =>
        _db.Tickets
            .Include(t => t.Department)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Include(t => t.RelatedAsset)
            .FirstOrDefaultAsync(t => t.Id == id);

    private static TicketDto ToDto(Ticket t) => new(
        t.Id, t.Title, t.Description, t.Priority, t.Status,
        t.Department?.Name ?? string.Empty,
        t.CreatedByUser?.FullName ?? string.Empty,
        t.AssignedToUserId, t.AssignedToUser?.FullName,
        t.RelatedAssetId, t.RelatedAsset?.AssetTag,
        t.CreatedAt, t.UpdatedAt, t.ClosedAt);
}

public record TicketCreateRequest(string Title, string Description, TicketPriority Priority, int? RelatedAssetId);
