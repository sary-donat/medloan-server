using System.ComponentModel.DataAnnotations;
using MedLoan.Api.Auth;
using MedLoan.Application.Loans;
using MedLoan.Application.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Controllers;

[Route("api/members")]
public class MembersController(IMemberService members, ILoanService loans) : ApiControllerBase
{
    private readonly IMemberService _members = members;
    private readonly ILoanService _loans = loans;

    // ct = default only because it follows an optional parameter; MVC always binds it from the request.
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetPaged([FromQuery, Range(1, int.MaxValue)] int page = 1, CancellationToken ct = default) =>
        Ok(await _members.GetPagedAsync(page, ct));

    // Any logged-in member (FallbackPolicy).
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct) =>
        ToActionResult(await _members.GetCurrentAsync(ct));

    // Full list, newest first, no paging.
    [HttpGet("me/loans")]
    public async Task<IActionResult> GetMyLoans(CancellationToken ct) =>
        ToActionResult(await _loans.GetMineAsync(ct));

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _members.GetByIdAsync(id, ct));

    [HttpPatch("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateMemberRequest request, CancellationToken ct) =>
        ToActionResult(await _members.UpdateAsync(id, request, ct));
}
