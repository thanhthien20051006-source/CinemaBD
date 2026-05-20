using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminInvoiceService
{
    Task<List<InvoiceDetail>> GetAllAsync(CancellationToken ct = default);
    Task<InvoiceDetail?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<CheckInResult> CheckInAsync(string qrText, CancellationToken ct = default);
    Task<InvoiceSyncReport> GetSyncReportAsync(CancellationToken ct = default);
    Task<InvoiceSyncReport> SyncAsync(CancellationToken ct = default);
}
