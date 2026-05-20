namespace CinemaBD.Domain.Entities;

public class Cinema
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Status { get; set; }
    public int RoomCount { get; set; }
}
