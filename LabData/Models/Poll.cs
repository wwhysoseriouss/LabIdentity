using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabData.Models;
public class Poll
{
    public Survey? Survey { get; set; }
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<PollCategory> Categories { get; set; } = new List<PollCategory>();
    public int Id { get; set; }
    public int? SurveyId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset OpensAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosesAtUtc { get; set; }
}

