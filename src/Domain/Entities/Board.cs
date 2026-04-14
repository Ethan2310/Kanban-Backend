namespace Domain.Entities;

public class Board : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<ProjectBoard> ProjectBoards { get; set; } = [];
    public ICollection<List> Lists { get; set; } = [];
    public ICollection<Task> Tasks { get; set; } = [];
}
