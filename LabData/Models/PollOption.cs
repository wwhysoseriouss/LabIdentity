using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabData.Models;

public class PollOption
{
    public Poll Poll { get; set; } = null!;
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public int Id { get; set; }
    public int PollId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}