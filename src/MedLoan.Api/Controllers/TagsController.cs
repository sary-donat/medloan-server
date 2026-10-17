using MedLoan.Api.Auth;
using MedLoan.Application.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Controllers;

[Route("api/tags")]
public class TagsController(ITagService tags) : ApiControllerBase
{
    private readonly ITagService _tags = tags;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _tags.GetAllAsync(ct));

    // There is no GET for a single tag, so Location points to the tag list.
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Create([FromBody] CreateTagRequest request, CancellationToken ct) =>
        ToCreatedResult(await _tags.CreateAsync(request, ct), nameof(GetAll), _ => new { });

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _tags.DeleteAsync(id, ct));
}
