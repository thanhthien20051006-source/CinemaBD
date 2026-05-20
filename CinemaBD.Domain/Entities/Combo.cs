namespace CinemaBD.Domain.Entities;

public class Combo
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}
