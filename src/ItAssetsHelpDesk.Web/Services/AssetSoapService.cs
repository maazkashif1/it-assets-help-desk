using ItAssetsHelpDesk.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ItAssetsHelpDesk.Web.Services;

public class AssetSoapService : IAssetSoapService
{
    private readonly ApplicationDbContext _db;

    public AssetSoapService(ApplicationDbContext db)
    {
        _db = db;
    }

    public AssetSoapDto GetAssetByTag(string assetTag)
    {
        var asset = _db.Assets
            .Include(a => a.Department)
            .Include(a => a.AssignedToUser)
            .AsNoTracking()
            .FirstOrDefault(a => a.AssetTag == assetTag);

        if (asset is null)
        {
            return new AssetSoapDto { Found = false, AssetTag = assetTag };
        }

        return new AssetSoapDto
        {
            Found = true,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            Type = asset.Type.ToString(),
            Status = asset.Status.ToString(),
            DepartmentName = asset.Department?.Name ?? string.Empty,
            AssignedToUserName = asset.AssignedToUser?.FullName,
            PurchaseCost = asset.PurchaseCost,
            PurchaseDate = asset.PurchaseDate,
            ExpiryDate = asset.ExpiryDate
        };
    }
}
