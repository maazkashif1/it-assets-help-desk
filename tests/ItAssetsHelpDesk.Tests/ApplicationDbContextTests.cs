using ItAssetsHelpDesk.Web.Data;
using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ItAssetsHelpDesk.Tests;

public class ApplicationDbContextTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Department SeedDepartment(ApplicationDbContext db)
    {
        var dept = new Department { Name = "IT" };
        db.Departments.Add(dept);
        db.SaveChanges();
        return dept;
    }

    [Fact]
    public void DeletingAsset_SoftDeletes_AndIsHiddenFromDefaultQuery()
    {
        using var db = CreateContext();
        var dept = SeedDepartment(db);
        var asset = new Asset { AssetTag = "T-1", Name = "Test Laptop", Type = AssetType.Laptop, DepartmentId = dept.Id, PurchaseCost = 100, PurchaseDate = DateTime.UtcNow };
        db.Assets.Add(asset);
        db.SaveChanges();

        db.Assets.Remove(asset);
        db.SaveChanges();

        Assert.Empty(db.Assets.Where(a => a.Id == asset.Id));

        var stillInDb = db.Assets.IgnoreQueryFilters().Single(a => a.Id == asset.Id);
        Assert.True(stillInDb.IsDeleted);
    }

    [Fact]
    public void CreatingAsset_WritesCreateAuditLog()
    {
        using var db = CreateContext();
        var dept = SeedDepartment(db);
        var asset = new Asset { AssetTag = "T-2", Name = "Test Printer", Type = AssetType.Printer, DepartmentId = dept.Id, PurchaseCost = 50, PurchaseDate = DateTime.UtcNow };
        db.Assets.Add(asset);
        db.SaveChanges();

        var log = Assert.Single(db.AuditLogs.Where(a => a.EntityName == nameof(Asset) && a.Action == AuditAction.Create));
        Assert.Equal(asset.Id.ToString(), log.EntityId);
        Assert.Contains("T-2", log.Details);
    }

    [Fact]
    public void UpdatingAsset_WritesUpdateAuditLogWithChangedFieldsOnly()
    {
        using var db = CreateContext();
        var dept = SeedDepartment(db);
        var asset = new Asset { AssetTag = "T-3", Name = "Old Name", Type = AssetType.Printer, DepartmentId = dept.Id, PurchaseCost = 50, PurchaseDate = DateTime.UtcNow };
        db.Assets.Add(asset);
        db.SaveChanges();

        asset.Name = "New Name";
        db.SaveChanges();

        var log = db.AuditLogs
            .Where(a => a.EntityName == nameof(Asset) && a.EntityId == asset.Id.ToString() && a.Action == AuditAction.Update)
            .OrderByDescending(a => a.Timestamp)
            .First();

        Assert.Contains("New Name", log.Details!);
        Assert.DoesNotContain("AssetTag", log.Details!);
    }

    [Fact]
    public void DeletingAsset_WritesDeleteAuditLog_NotUpdate()
    {
        using var db = CreateContext();
        var dept = SeedDepartment(db);
        var asset = new Asset { AssetTag = "T-4", Name = "To Delete", Type = AssetType.NetworkGear, DepartmentId = dept.Id, PurchaseCost = 10, PurchaseDate = DateTime.UtcNow };
        db.Assets.Add(asset);
        db.SaveChanges();

        db.Assets.Remove(asset);
        db.SaveChanges();

        var log = db.AuditLogs
            .Where(a => a.EntityName == nameof(Asset) && a.EntityId == asset.Id.ToString())
            .OrderByDescending(a => a.Timestamp)
            .First();

        Assert.Equal(AuditAction.Delete, log.Action);
    }

    [Fact]
    public void AuditLog_NeverContainsPasswordHash()
    {
        using var db = CreateContext();
        var user = new ApplicationUser { UserName = "a@b.com", Email = "a@b.com", FullName = "A B", PasswordHash = "super-secret-hash" };
        db.Users.Add(user);
        db.SaveChanges();

        var log = Assert.Single(db.AuditLogs.Where(a => a.EntityName == nameof(ApplicationUser)));
        Assert.DoesNotContain("super-secret-hash", log.Details!);
    }
}
