using System.ComponentModel.DataAnnotations;

namespace LabIdentity.ViewModels;

public class PollFormViewModel
{
    [Display(Name = "Питання голосування")]
    [Required(ErrorMessage = "QuestionRequired")]
    [StringLength(
        200,
        MinimumLength = 5,
        ErrorMessage = "QuestionLength")]
    public string Question { get; set; } = string.Empty;

    [Display(Name = "Опис")]
    [StringLength(
        2000,
        ErrorMessage = "DescriptionLength")]
    public string? Description { get; set; }

    [Display(Name = "Тривалість голосування, годин")]
    [Required(ErrorMessage = "DurationRequired")]
    [Range(
        1,
        720,
        ErrorMessage = "DurationRange")]
    public int? DurationHours { get; set; } = 24;

    [Display(Name = "Варіанти відповіді — кожен із нового рядка")]
    [Required(ErrorMessage = "OptionsRequired")]
    [StringLength(
        10000,
        ErrorMessage = "OptionsLength")]
    public string OptionsText { get; set; } = string.Empty;
}