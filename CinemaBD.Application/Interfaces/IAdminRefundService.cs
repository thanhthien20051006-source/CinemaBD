using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminRefundService
{
    Task<List<RefundRequest>> GetAllAsync(string? status = null, CancellationToken ct = default);
    Task<RefundRequestResult> ApproveAsync(int id, string? adminNote, CancellationToken ct = default);
    Task<RefundRequestResult> RejectAsync(int id, string? adminNote, CancellationToken ct = default);
}
