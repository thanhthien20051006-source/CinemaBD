using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface ILoyaltyPointService
{
    Task<LoyaltyPoint> GetOrCreateAsync(string customerId, CancellationToken ct = default);
    Task<int> EarnFromPaymentAsync(string txnRef, CancellationToken ct = default);
    Task<LoyaltyRedeemResult> PreviewRedeemAsync(string customerId, int points, decimal subtotal, CancellationToken ct = default);
    Task<LoyaltyRedeemResult> RedeemAsync(string customerId, int points, decimal subtotal, CancellationToken ct = default);
    Task<int> RefundRedeemedPointsAsync(string txnRef, CancellationToken ct = default);
}


