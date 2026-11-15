namespace CinemaBD.Application.Interfaces;

public interface IAccountPasswordService
{
    Task ChangeCustomerPasswordAsync(string customerId, string currentPassword, string newPassword, string? currentJti, DateTime? currentTokenExpiresAtUtc, CancellationToken cancellationToken = default);
    Task ChangeAdminPasswordAsync(int adminId, string currentPassword, string newPassword, string? currentJti, DateTime? currentTokenExpiresAtUtc, CancellationToken cancellationToken = default);
}
