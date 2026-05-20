namespace CinemaBD.Web.Models;

public class ComboSelectionPageViewModel
{
    public string ShowtimeId { get; set; } = string.Empty;
    public IReadOnlyList<string> Seats { get; set; } = Array.Empty<string>();
    public decimal TicketTotal { get; set; }
    public IReadOnlyList<ComboViewModel> Combos { get; set; } = Array.Empty<ComboViewModel>();
}
