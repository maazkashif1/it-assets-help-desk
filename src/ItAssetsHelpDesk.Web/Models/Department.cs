namespace ItAssetsHelpDesk.Web.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
