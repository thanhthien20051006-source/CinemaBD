using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminEmployeeService
{
    Task<IReadOnlyCollection<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<Employee?> UpdateAsync(int id, Employee employee, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
