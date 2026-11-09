using ItAssetsHelpDesk.Web.Models.Enums;

namespace ItAssetsHelpDesk.Web.Models;

public class Asset : ISoftDelete
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Active;

    public string? SerialNumber { get; set; }

    // Only meaningful for Type == SoftwareLicense
    public int? SeatsTotal { get; set; }
    public int? SeatsUsed { get; set; }

    public decimal PurchaseCost { get; set; }
    public DateTime PurchaseDate { get; set; }

    // Warranty expiry for hardware, license expiry for software licenses
    public DateTime? ExpiryDate { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
