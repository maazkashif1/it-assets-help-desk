using ItAssetsHelpDesk.Web.Models.Enums;

namespace ItAssetsHelpDesk.Web.Controllers.Api;

public record AssetDto(
    int Id,
    string AssetTag,
    string Name,
    AssetType Type,
    AssetStatus Status,
    string? SerialNumber,
    int? SeatsTotal,
    int? SeatsUsed,
    decimal PurchaseCost,
    DateTime PurchaseDate,
    DateTime? ExpiryDate,
    int DepartmentId,
    string? DepartmentName,
    string? AssignedToUserId,
    string? AssignedToUserName);

public record AssetRequest(
    string AssetTag,
    string Name,
    AssetType Type,
    AssetStatus Status,
    string? SerialNumber,
    int? SeatsTotal,
    int? SeatsUsed,
    decimal PurchaseCost,
    DateTime PurchaseDate,
    DateTime? ExpiryDate,
    int DepartmentId,
    string? AssignedToUserId);

public record TicketDto(
    int Id,
    string Title,
    string Description,
    TicketPriority Priority,
    TicketStatus Status,
    string DepartmentName,
    string CreatedByUserName,
    string? AssignedToUserId,
    string? AssignedToUserName,
    int? RelatedAssetId,
    string? RelatedAssetTag,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt);

public record TicketUpdateRequest(
    TicketStatus? Status,
    TicketPriority? Priority,
    string? AssignedToUserId);
