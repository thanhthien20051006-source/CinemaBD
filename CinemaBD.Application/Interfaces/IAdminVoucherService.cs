using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminVoucherService
{
    Task<List<Voucher>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Voucher?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Voucher> UpsertAsync(string id, string customerId, string code, string? description, DateTime expiredAt, decimal discountValue, string discountType, decimal minOrderAmount, decimal maxDiscountAmount, CancellationToken ct = default);
    Task<VoucherValidationResult> ValidateAsync(string customerId, string code, decimal subtotal, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
