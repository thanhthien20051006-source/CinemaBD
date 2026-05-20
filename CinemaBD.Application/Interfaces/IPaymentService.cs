using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentCallbackResult> HandleVnPayReturnAsync(IDictionary<string, string> query, CancellationToken cancellationToken = default);
    Task<VnPayIpnResult> HandleVnPayIpnAsync(IDictionary<string, string> query, CancellationToken cancellationToken = default);
    Task<PaymentCallbackResult> ConfirmDemoPaymentAsync(string txnRef, string gateway, CancellationToken cancellationToken = default);
}
