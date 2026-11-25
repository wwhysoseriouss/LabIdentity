using System.ComponentModel.DataAnnotations;

namespace LabIdentity.ViewModels;

public class PollFormViewModel
{
    [Display(Name = "Питання голосування")]
    [Required(ErrorMessage = "Введіть питання.")]
    [StringLength(200, MinimumLength = 5,
        ErrorMessage = "Питання повинно містити від 5 до 200 символів.")]
    public string Question { get; set; } = string.Empty;

    [Display(Name = "Опис")]
    [StringLength(2000,
        ErrorMessage = "Опис не може перевищувати 2000 символів.")]
    public string? Description { get; set; }

    [Display(Name = "Тривалість голосування, годин")]
    [Required(ErrorMessage = "Вкажіть тривалість.")]
    [Range(1, 720,
        ErrorMessage = "Тривалість повинна бути від 1 до 720 годин.")]
    public int? DurationHours { get; set; } = 24;

    [Display(Name = "Варіанти відповіді — кожен із нового рядка")]
    [Required(ErrorMessage = "Введіть варіанти відповіді.")]
    [StringLength(10000,
        ErrorMessage = "Список варіантів занадто довгий.")]
    public string OptionsText { get; set; } = string.Empty;
}