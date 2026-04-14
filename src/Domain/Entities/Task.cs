using Domain.Enumerations;

namespace Domain.Entities;

public class Task : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int BoardId { get; set; }
    public int ListId { get; set; }
    public int StatusId { get; set; }
    public int? AssignedUserId { get; set; }
    public int OrderIndex { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }

    public Board Board { get; set; } = null!;
    public List List { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public User? AssignedUser { get; set; }
}
