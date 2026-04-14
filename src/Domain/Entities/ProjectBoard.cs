namespace Domain.Entities;

public class ProjectBoard : BaseEntity
{
    public int ProjectId { get; set; }
    public int BoardId { get; set; }

    public Project Project { get; set; } = null!;
    public Board Board { get; set; } = null!;
}
