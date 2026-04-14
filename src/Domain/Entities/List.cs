namespace Domain.Entities;

public class List : BaseEntity
{
    public string Name { get; set; } = null!;
    public int BoardId { get; set; }
    public int StatusId { get; set; }
    public int OrderIndex { get; set; }

    public Board Board { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public ICollection<Task> Tasks { get; set; } = [];
}
