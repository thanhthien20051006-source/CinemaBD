namespace CinemaBD.Web.Models;

public record AdminListPageViewModel(string Title, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

public record AdminMetricCard(string Label, string Value);

public class AdminThongKeViewModel
{
    public IReadOnlyList<AdminMetricCard> Cards { get; set; } = new List<AdminMetricCard>();
}
