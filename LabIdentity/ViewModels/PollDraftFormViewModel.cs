using System.ComponentModel.DataAnnotations;

namespace LabIdentity.ViewModels;

public class PollDraftFormViewModel
{
    [Required(ErrorMessage = "QuestionRequired")]
    [StringLength(200, ErrorMessage = "QuestionMaxLength")]
    public string Question { get; set; } = string.Empty;
}