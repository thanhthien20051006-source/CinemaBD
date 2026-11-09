using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IVoucherPreviewService
{
    Task<VoucherValidationResult> PreviewAsync(
        string customerId,
        string showtimeId,
        IReadOnlyCollection<string> seats,
        string? combos,
        string code,
        CancellationToken cancellationToken = default);
}
