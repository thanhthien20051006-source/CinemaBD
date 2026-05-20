namespace CinemaBD.Domain.Entities;

public class LoyaltyPoint
{
    public string Id { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string? CustomerName { get; set; }
    public int RewardPoints { get; set; }
    public int EarnedPoints { get; set; }
    public int UsedPoints { get; set; }
    public int Balance => RewardPoints + EarnedPoints - UsedPoints;
    public int PointsToMoney { get; set; }
    public int MoneyPerPoint { get; set; } = 10000;
    public int PointValue { get; set; } = 1000;
}

public class LoyaltyRedeemResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UsedPoints { get; set; }
    public decimal DiscountAmount { get; set; }
    public int RemainingPoints { get; set; }
}
