using LabData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabData.Models;

public class Vote
{
    public Poll Poll { get; set; } = null!;
    public PollOption PollOption { get; set; } = null!;
    public ApplicationUser Voter { get; set; } = null!;
    public int Id { get; set; }
    public int PollId { get; set; }
    public int PollOptionId { get; set; }
    public string VoterId { get; set; } = string.Empty;
    public DateTimeOffset CastAtUtc { get; set; } = DateTimeOffset.UtcNow;
}