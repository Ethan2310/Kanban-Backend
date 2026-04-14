using Domain.ValueObjects;

namespace Domain.Entities;

public class Status : BaseEntity
{
    public string Name { get; set; } = null!;
    public HexColor? Color { get; set; }
    public int OrderIndex { get; set; }

    public ICollection<List> Lists { get; set; } = [];
    public ICollection<Task> Tasks { get; set; } = [];
}
