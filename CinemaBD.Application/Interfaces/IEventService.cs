using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IEventService
{
    Task<List<Event>> GetActiveAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
}
