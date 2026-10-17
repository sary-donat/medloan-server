using MedLoan.Api.Auth;
using MedLoan.Application.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Controllers;

[Route("api/categories")]
public class CategoriesController(ICategoryService categories) : ApiControllerBase
{
    private readonly ICategoryService _categories = categories;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _categories.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _categories.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct) =>
        ToCreatedResult(await _categories.CreateAsync(request, ct), nameof(GetById), category => new { id = category.Id });

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct) =>
        ToActionResult(await _categories.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _categories.DeleteAsync(id, ct));
}
