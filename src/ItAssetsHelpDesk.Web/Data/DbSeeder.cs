using ItAssetsHelpDesk.Web.Models;
using ItAssetsHelpDesk.Web.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ItAssetsHelpDesk.Web.Data;

// Applies pending migrations and seeds roles, demo accounts, and sample
// assets/tickets so the dashboard has realistic data on first run.
public static class DbSeeder
{
    public const string DemoPassword = "Passw0rd!123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (await db.Departments.AnyAsync())
        {
            return;
        }

        var departments = new[]
        {
            new Department { Name = "IT" },
            new Department { Name = "Finance" },
            new Department { Name = "HR" },
            new Department { Name = "Sales" },
            new Department { Name = "Operations" },
        };
        db.Departments.AddRange(departments);
        await db.SaveChangesAsync();

        var (it, finance, hr, sales, ops) = (departments[0], departments[1], departments[2], departments[3], departments[4]);

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await CreateUserAsync(userManager, "admin@itassets.local", "Ava Admin", it, AppRoles.Admin);
        var itStaff1 = await CreateUserAsync(userManager, "itstaff1@itassets.local", "Ian Support", it, AppRoles.ITStaff);
        var itStaff2 = await CreateUserAsync(userManager, "itstaff2@itassets.local", "Priya Patel", it, AppRoles.ITStaff);
        var emp1 = await CreateUserAsync(userManager, "employee1@itassets.local", "Sam Finance", finance, AppRoles.Employee);
        var emp2 = await CreateUserAsync(userManager, "employee2@itassets.local", "Dana HR", hr, AppRoles.Employee);
        var emp3 = await CreateUserAsync(userManager, "employee3@itassets.local", "Theo Sales", sales, AppRoles.Employee);

        var today = DateTime.UtcNow.Date;

        var assets = new List<Asset>
        {
            new() { AssetTag = "LAP-1001", Name = "Dell Latitude 5440", Type = AssetType.Laptop, Status = AssetStatus.Active, SerialNumber = "DL5440-001", PurchaseCost = 1200, PurchaseDate = today.AddYears(-2), ExpiryDate = today.AddDays(12), DepartmentId = it.Id, AssignedToUserId = itStaff1.Id },
            new() { AssetTag = "LAP-1002", Name = "MacBook Pro 14\"", Type = AssetType.Laptop, Status = AssetStatus.Active, SerialNumber = "MBP14-002", PurchaseCost = 2400, PurchaseDate = today.AddYears(-1), ExpiryDate = today.AddDays(200), DepartmentId = finance.Id, AssignedToUserId = emp1.Id },
            new() { AssetTag = "LAP-1003", Name = "Lenovo ThinkPad T14", Type = AssetType.Laptop, Status = AssetStatus.Active, SerialNumber = "TP14-003", PurchaseCost = 1100, PurchaseDate = today.AddYears(-3), ExpiryDate = today.AddDays(-30), DepartmentId = hr.Id, AssignedToUserId = emp2.Id },
            new() { AssetTag = "LAP-1004", Name = "Dell Latitude 5440", Type = AssetType.Laptop, Status = AssetStatus.InRepair, SerialNumber = "DL5440-004", PurchaseCost = 1200, PurchaseDate = today.AddMonths(-8), ExpiryDate = today.AddDays(400), DepartmentId = sales.Id, AssignedToUserId = emp3.Id },
            new() { AssetTag = "PRN-2001", Name = "HP LaserJet Pro M404", Type = AssetType.Printer, Status = AssetStatus.Active, SerialNumber = "HPM404-001", PurchaseCost = 350, PurchaseDate = today.AddYears(-2), ExpiryDate = today.AddDays(25), DepartmentId = ops.Id },
            new() { AssetTag = "PRN-2002", Name = "Canon imageCLASS MF445dw", Type = AssetType.Printer, Status = AssetStatus.Active, SerialNumber = "CANMF445-002", PurchaseCost = 420, PurchaseDate = today.AddYears(-1), ExpiryDate = today.AddDays(500), DepartmentId = it.Id },
            new() { AssetTag = "NET-3001", Name = "Cisco Catalyst 9200 Switch", Type = AssetType.NetworkGear, Status = AssetStatus.Active, SerialNumber = "C9200-001", PurchaseCost = 3200, PurchaseDate = today.AddYears(-2), ExpiryDate = today.AddDays(60), DepartmentId = it.Id },
            new() { AssetTag = "NET-3002", Name = "Ubiquiti UniFi Access Point", Type = AssetType.NetworkGear, Status = AssetStatus.Active, SerialNumber = "UAP-002", PurchaseCost = 180, PurchaseDate = today.AddMonths(-6), ExpiryDate = today.AddDays(730), DepartmentId = ops.Id },
            new() { AssetTag = "LIC-4001", Name = "Microsoft 365 Business", Type = AssetType.SoftwareLicense, Status = AssetStatus.Active, PurchaseCost = 1800, PurchaseDate = today.AddMonths(-10), ExpiryDate = today.AddDays(18), SeatsTotal = 50, SeatsUsed = 42, DepartmentId = it.Id },
            new() { AssetTag = "LIC-4002", Name = "Adobe Creative Cloud", Type = AssetType.SoftwareLicense, Status = AssetStatus.Active, PurchaseCost = 960, PurchaseDate = today.AddMonths(-4), ExpiryDate = today.AddDays(5), SeatsTotal = 10, SeatsUsed = 9, DepartmentId = sales.Id },
            new() { AssetTag = "LIC-4003", Name = "Slack Business+", Type = AssetType.SoftwareLicense, Status = AssetStatus.Active, PurchaseCost = 600, PurchaseDate = today.AddMonths(-2), ExpiryDate = today.AddDays(300), SeatsTotal = 100, SeatsUsed = 88, DepartmentId = hr.Id },
        };
        db.Assets.AddRange(assets);
        await db.SaveChangesAsync();

        var tickets = new List<Ticket>
        {
            new() { Title = "Laptop won't boot", Description = "Dell Latitude shows a black screen after the Windows update.", Priority = TicketPriority.Critical, Status = TicketStatus.Open, CreatedByUserId = emp2.Id, DepartmentId = hr.Id, RelatedAssetId = assets[2].Id, CreatedAt = DateTime.UtcNow.AddHours(-3) },
            new() { Title = "Need Adobe Creative Cloud seat", Description = "New hire on the design team needs a license assigned.", Priority = TicketPriority.Medium, Status = TicketStatus.InProgress, CreatedByUserId = emp3.Id, AssignedToUserId = itStaff2.Id, DepartmentId = sales.Id, RelatedAssetId = assets[9].Id, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Title = "Printer jamming repeatedly", Description = "HP LaserJet in Ops jams on every duplex print job.", Priority = TicketPriority.High, Status = TicketStatus.Open, CreatedByUserId = emp1.Id, DepartmentId = ops.Id, RelatedAssetId = assets[4].Id, CreatedAt = DateTime.UtcNow.AddHours(-10) },
            new() { Title = "VPN keeps disconnecting", Description = "Drops every ~15 minutes when working from home.", Priority = TicketPriority.Medium, Status = TicketStatus.Open, CreatedByUserId = emp1.Id, DepartmentId = finance.Id, CreatedAt = DateTime.UtcNow.AddHours(-20) },
            new() { Title = "Request new monitor", Description = "Second monitor for the new desk setup.", Priority = TicketPriority.Low, Status = TicketStatus.Resolved, CreatedByUserId = emp2.Id, AssignedToUserId = itStaff1.Id, DepartmentId = hr.Id, CreatedAt = DateTime.UtcNow.AddDays(-5), UpdatedAt = DateTime.UtcNow.AddDays(-4), ClosedAt = DateTime.UtcNow.AddDays(-4) },
            new() { Title = "Switch port not working", Description = "Port 14 on the Catalyst switch isn't passing traffic.", Priority = TicketPriority.High, Status = TicketStatus.Closed, CreatedByUserId = itStaff1.Id, AssignedToUserId = itStaff2.Id, DepartmentId = it.Id, RelatedAssetId = assets[6].Id, CreatedAt = DateTime.UtcNow.AddDays(-9), UpdatedAt = DateTime.UtcNow.AddDays(-8), ClosedAt = DateTime.UtcNow.AddDays(-8) },
            new() { Title = "Password reset for shared mailbox", Description = "Locked out of the hr-recruiting@ mailbox.", Priority = TicketPriority.Low, Status = TicketStatus.Open, CreatedByUserId = emp2.Id, DepartmentId = hr.Id, CreatedAt = DateTime.UtcNow.AddHours(-1) },
        };
        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string fullName, Department department, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            DepartmentId = department.Id
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
