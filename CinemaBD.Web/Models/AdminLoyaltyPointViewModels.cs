namespace CinemaBD.Web.Models;

public class AdminLoyaltyPointViewModel
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public int RewardPoints { get; set; }
    public int EarnedPoints { get; set; }
    public int UsedPoints { get; set; }
    public int Balance { get; set; }
    public int PointsToMoney { get; set; }
    public int MoneyPerPoint { get; set; }
    public int PointValue { get; set; }
}

public class AdminLoyaltyAdjustViewModel
{
    public string CustomerId { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Action { get; set; } = "add";
}
