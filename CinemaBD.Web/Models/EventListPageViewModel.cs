namespace CinemaBD.Web.Models;

public class EventListPageViewModel
{
    public IReadOnlyList<EventViewModel> Events { get; set; } = Array.Empty<EventViewModel>();
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
