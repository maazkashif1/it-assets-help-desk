using System.Security.Claims;
using System.Text.Json;
using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.Models.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ItAssetsHelpDesk.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DepartmentAssetCost> DepartmentAssetCosts => Set<DepartmentAssetCost>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Asset>().HasQueryFilter(a => !a.IsDeleted);
        builder.Entity<Ticket>().HasQueryFilter(t => !t.IsDeleted);

        builder.Entity<Asset>()
            .HasIndex(a => a.AssetTag)
            .IsUnique();

        builder.Entity<Asset>()
            .Property(a => a.PurchaseCost)
            .HasColumnType("decimal(18,2)");

        // Restrict FK delete behavior everywhere to avoid SQL Server's
        // "multiple cascade paths" error across Department/User/Asset/Ticket.
        builder.Entity<Asset>()
            .HasOne(a => a.Department)
            .WithMany(d => d.Assets)
            .HasForeignKey(a => a.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Asset>()
            .HasOne(a => a.AssignedToUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.Department)
            .WithMany(d => d.Tickets)
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ticket>()
            .HasOne(t => t.RelatedAsset)
            .WithMany()
            .HasForeignKey(t => t.RelatedAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DepartmentAssetCost>(cost =>
        {
            cost.HasNoKey().ToView(null);
            cost.Property(c => c.TotalCost).HasColumnType("decimal(18,2)");
        });
    }

    // DbContext.SaveChanges() forwards to this overload, so overriding it
    // (rather than just SaveChangesAsync) means auditing/soft-delete can't
    // be silently bypassed by a sync call.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var pendingAudits = CapturePendingAudits();

        var result = base.SaveChanges(acceptAllChangesOnSuccess);

        if (pendingAudits.Count > 0)
        {
            foreach (var (entry, audit) in pendingAudits)
            {
                audit.EntityId = GetPrimaryKeyValue(entry);
                AuditLogs.Add(audit);
            }
            base.SaveChanges(acceptAllChangesOnSuccess);
        }

        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pendingAudits = CapturePendingAudits();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (pendingAudits.Count > 0)
        {
            foreach (var (entry, audit) in pendingAudits)
            {
                audit.EntityId = GetPrimaryKeyValue(entry);
                AuditLogs.Add(audit);
            }
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private List<(EntityEntry Entry, AuditLog Audit)> CapturePendingAudits()
    {
        var userId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "system";

        var pending = new List<(EntityEntry, AuditLog)>();

        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            if (!IsAudited(entry.Entity)) continue;

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDelete softDelete)
            {
                entry.State = EntityState.Modified;
                softDelete.IsDeleted = true;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Deleted => AuditAction.Delete,
                EntityState.Modified when entry.Entity is ISoftDelete { IsDeleted: true } && entry.Property(nameof(ISoftDelete.IsDeleted)).IsModified
                    => AuditAction.Delete,
                EntityState.Modified => AuditAction.Update,
                _ => (AuditAction?)null
            };

            if (action is null) continue;

            pending.Add((entry, new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action.Value,
                EntityName = entry.Entity.GetType().Name,
                Details = BuildDetails(entry, action.Value),
                Timestamp = DateTime.UtcNow
            }));
        }

        return pending;
    }

    private static bool IsAudited(object entity) =>
        entity is Asset or Ticket or Department or ApplicationUser;

    private static string GetPrimaryKeyValue(EntityEntry entry) =>
        entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? string.Empty;

    // Identity bookkeeping fields that should never land in the audit trail.
    private static readonly HashSet<string> SensitiveProperties = new()
    {
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp"
    };

    private static string BuildDetails(EntityEntry entry, AuditAction action)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (SensitiveProperties.Contains(name)) continue;
            // Primary key is already captured in the EntityId column; for a
            // new row EF only assigns the real value after SaveChanges.
            if (property.Metadata.IsPrimaryKey()) continue;
            if (action == AuditAction.Update && !property.IsModified) continue;

            values[name] = action == AuditAction.Update
                ? new { Old = property.OriginalValue, New = property.CurrentValue }
                : property.CurrentValue;
        }

        return JsonSerializer.Serialize(values);
    }
}
