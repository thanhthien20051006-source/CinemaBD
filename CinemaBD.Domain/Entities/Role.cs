namespace CinemaBD.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsMaster { get; set; }
    public bool IsActive { get; set; }
}
