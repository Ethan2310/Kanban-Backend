namespace Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public int CreatedById { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? UpdatedById { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool IsActive { get; set; } = true;
}
