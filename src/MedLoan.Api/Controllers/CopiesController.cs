using MedLoan.Api.Auth;
using MedLoan.Application.Copies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Controllers;

// Two route roots: copies are listed and added under their item, and updated by their own id.
public class CopiesController(ICopyService copies) : ApiControllerBase
{
    private readonly ICopyService _copies = copies;

    [HttpGet("api/items/{itemId:int}/copies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByItem([FromRoute] int itemId, CancellationToken ct) =>
        ToActionResult(await _copies.GetByItemIdAsync(itemId, ct));

    // There is no GET for a single copy, so Location points to the item's copy list.
    [HttpPost("api/items/{itemId:int}/copies")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Create([FromRoute] int itemId, [FromBody] CreateCopyRequest request, CancellationToken ct) =>
        ToCreatedResult(await _copies.CreateAsync(itemId, request, ct), nameof(GetByItem), _ => new { itemId });

    [HttpPatch("api/copies/{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCopyRequest request, CancellationToken ct) =>
        ToActionResult(await _copies.UpdateAsync(id, request, ct));
}
