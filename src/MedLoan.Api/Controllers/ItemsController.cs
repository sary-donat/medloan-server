using MedLoan.Application.Items;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Controllers;

[Route("api/items")]
public class ItemsController(IItemService items) : ApiControllerBase
{
    private readonly IItemService _items = items;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] ItemQueryParameters query, CancellationToken ct) =>
        Ok(await _items.GetPagedAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _items.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest request, CancellationToken ct) =>
        ToCreatedResult(await _items.CreateAsync(request, ct), nameof(GetById), item => new { id = item.Id });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateItemRequest request, CancellationToken ct) =>
        ToActionResult(await _items.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _items.DeleteAsync(id, ct));
}
