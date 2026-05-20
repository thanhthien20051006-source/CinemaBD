using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IComboService
{
    Task<List<Combo>> GetAllAsync(CancellationToken ct = default);
}
