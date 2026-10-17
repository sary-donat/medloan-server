using System.ComponentModel.DataAnnotations;

namespace MedLoan.Application.Tags;

public sealed record CreateTagRequest
{
    [Required, StringLength(50)]
    public string Name { get; init; } = string.Empty;
}
