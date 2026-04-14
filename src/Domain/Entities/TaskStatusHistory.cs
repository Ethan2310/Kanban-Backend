namespace Domain.Entities;

/// <summary>
/// Append-only audit log — never updated or deleted after insert.
/// Does not inherit BaseEntity. Navigation properties are intentionally omitted
/// to prevent global soft-delete query filters from hiding rows when related
/// entities are deactivated.
/// </summary>
public class TaskStatusHistory
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int? StatusChangedFrom { get; set; }
    public int StatusChangedTo { get; set; }
    public int ChangedById { get; set; }
    public DateTime ChangedAt { get; set; }
}
