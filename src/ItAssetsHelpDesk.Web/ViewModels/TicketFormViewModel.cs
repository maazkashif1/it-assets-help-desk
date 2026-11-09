using System.ComponentModel.DataAnnotations;
using ItAssetsHelpDesk.Web.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ItAssetsHelpDesk.Web.ViewModels;

public class TicketFormViewModel
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Display(Name = "Related Asset (optional)")]
    public int? RelatedAssetId { get; set; }

    public List<SelectListItem> AssetOptions { get; set; } = new();
}
