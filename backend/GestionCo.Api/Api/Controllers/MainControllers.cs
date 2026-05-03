using GestionCo.Api.Application.Categories;
using GestionCo.Api.Application.Clients;
using GestionCo.Api.Application.Fournisseurs;
using GestionCo.Api.Application.Produits.Commands;
using GestionCo.Api.Application.Produits.DTOs;
using GestionCo.Api.Application.Produits.Queries;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCo.Api.Api.Controllers;

[ApiController]
[Route("api/produits")]
[Authorize(Policy = "AdminOrManager")]
public class ProduitsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProduitsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetProduitsQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProduitByIdQuery(id), ct));

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => Ok(await _mediator.Send(new GetProduitsStatsQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProduitDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateProduitCommand(dto), ct));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProduitDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return Ok(await _mediator.Send(new UpdateProduitCommand(dto), ct));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProduitCommand(id), ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/clients")]
[Authorize(Policy = "AdminOrManager")]
public class ClientsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ClientsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetClientsQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetClientByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateClientCommand(dto), ct));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return Ok(await _mediator.Send(new UpdateClientCommand(dto), ct));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteClientCommand(id), ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/fournisseurs")]
[Authorize(Policy = "AdminOrManager")]
public class FournisseursController : ControllerBase
{
    private readonly IMediator _mediator;
    public FournisseursController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetFournisseursQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetFournisseurByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFournisseurDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateFournisseurCommand(dto), ct));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFournisseurDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return Ok(await _mediator.Send(new UpdateFournisseurCommand(dto), ct));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteFournisseurCommand(id), ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCategoriesQuery(), ct));

    [HttpPost]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Create([FromBody] CreateCategorieDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCategorieCommand(dto), ct));

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCategorieDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateCategorieCommand(id, dto), ct));

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCategorieCommand(id), ct);
        return NoContent();
    }
}
