using LabData.Data;

namespace LabData.Models;

public class PollDraft
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;
}