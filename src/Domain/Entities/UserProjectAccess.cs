namespace Domain.Entities;

public class UserProjectAccess : BaseEntity
{
    public int UserId { get; set; }
    public int ProjectId { get; set; }

    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
