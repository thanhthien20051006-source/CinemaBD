using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface ICustomerProfileService
{
    Task<Customer?> GetProfileAsync(string customerId, CancellationToken ct = default);
    Task<Customer?> UpdateProfileAsync(string customerId, CustomerProfileUpdate profile, CancellationToken ct = default);
    Task<List<CustomerHistory>> GetHistoryAsync(string customerId, CancellationToken ct = default);
    Task<decimal> GetTotalSpendingAsync(string customerId, int year, CancellationToken ct = default);
}
