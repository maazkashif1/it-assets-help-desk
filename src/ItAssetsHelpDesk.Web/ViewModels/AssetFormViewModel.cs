using System.ComponentModel.DataAnnotations;
using ItAssetsHelpDesk.Web.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ItAssetsHelpDesk.Web.ViewModels;

public class AssetFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    [Display(Name = "Asset Tag")]
    public string AssetTag { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AssetType Type { get; set; }

    [Required]
    public AssetStatus Status { get; set; } = AssetStatus.Active;

    [Display(Name = "Serial Number")]
    public string? SerialNumber { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Seats Total")]
    public int? SeatsTotal { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Seats Used")]
    public int? SeatsUsed { get; set; }

    [Required, Range(0, 100000000)]
    [Display(Name = "Purchase Cost")]
    public decimal PurchaseCost { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Purchase Date")]
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow.Date;

    [DataType(DataType.Date)]
    [Display(Name = "Warranty / License Expiry")]
    public DateTime? ExpiryDate { get; set; }

    [Required]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedToUserId { get; set; }

    public List<SelectListItem> DepartmentOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();
}
