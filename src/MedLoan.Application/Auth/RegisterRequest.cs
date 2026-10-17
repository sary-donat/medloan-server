using System.ComponentModel.DataAnnotations;

namespace MedLoan.Application.Auth;

// No Role field: a new member is always a Member.
public sealed record RegisterRequest
{
    [Required, StringLength(100)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; init; } = string.Empty;

    [Required, MinLength(8), StringLength(100)]
    public string Password { get; init; } = string.Empty;
}
