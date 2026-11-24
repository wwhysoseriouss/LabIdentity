using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace LabIdentity.ViewModels;

public class RegisterParticipantViewModel
{
    [Display(Name = "Ім’я")]
    [Required(ErrorMessage = "Required")]
    [StringLength(50, MinimumLength = 2,
        ErrorMessage = "StringLength")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Прізвище")]
    [Required(ErrorMessage = "Required")]
    [StringLength(50, MinimumLength = 2,
        ErrorMessage = "StringLength")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Електронна пошта")]
    [Required(ErrorMessage = "Required")]
    [EmailAddress(ErrorMessage = "InvalidEmail")]
    [StringLength(256, ErrorMessage = "MaxLength")]
    [Remote(
        action: "VerifyEmail",
        controller: "Participants",
        ErrorMessage = "EmailTaken")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Вік")]
    [Required(ErrorMessage = "Required")]
    [Range(18, 120, ErrorMessage = "AgeRange")]
    public int? Age { get; set; }

    [Display(Name = "Пароль")]
    [Required(ErrorMessage = "Required")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "StringLength")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Підтвердження пароля")]
    [Required(ErrorMessage = "Required")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password),
        ErrorMessage = "PasswordMismatch")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Код участі (необов’язково)")]
    [StringLength(30, ErrorMessage = "MaxLength")]
    [Remote(
        action: "VerifyParticipationCode",
        controller: "Participants",
        AdditionalFields = nameof(Age),
        ErrorMessage = "InvalidParticipationCode")]
    public string? ParticipationCode { get; set; }
}