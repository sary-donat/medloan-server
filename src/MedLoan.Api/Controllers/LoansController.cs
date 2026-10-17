using MedLoan.Api.Auth;
using MedLoan.Application.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Controllers;

// Any logged-in member (FallbackPolicy). Ownership of a single loan is checked in LoanService (403).
[Route("api/loans")]
public class LoansController(ILoanService loans) : ApiControllerBase
{
    private readonly ILoanService _loans = loans;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLoanRequest request, CancellationToken ct) =>
        ToCreatedResult(await _loans.CreateAsync(request, ct), nameof(GetById), loan => new { id = loan.Id });

    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetPaged([FromQuery] LoanQueryParameters query, CancellationToken ct) =>
        Ok(await _loans.GetPagedAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _loans.GetByIdAsync(id, ct));

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Return([FromRoute] int id, [FromBody] UpdateLoanRequest request, CancellationToken ct) =>
        ToActionResult(await _loans.ReturnAsync(id, request, ct));
}
