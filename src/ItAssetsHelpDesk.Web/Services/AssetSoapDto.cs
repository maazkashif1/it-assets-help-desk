using System.Runtime.Serialization;

namespace ItAssetsHelpDesk.Web.Services;

[DataContract]
public class AssetSoapDto
{
    [DataMember] public bool Found { get; set; }
    [DataMember] public string AssetTag { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string Type { get; set; } = string.Empty;
    [DataMember] public string Status { get; set; } = string.Empty;
    [DataMember] public string DepartmentName { get; set; } = string.Empty;
    [DataMember] public string? AssignedToUserName { get; set; }
    [DataMember] public decimal PurchaseCost { get; set; }
    [DataMember] public DateTime PurchaseDate { get; set; }
    [DataMember] public DateTime? ExpiryDate { get; set; }
}
