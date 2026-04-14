namespace Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<ProjectBoard> ProjectBoards { get; set; } = [];
    public ICollection<UserProjectAccess> UserProjectAccesses { get; set; } = [];
}
