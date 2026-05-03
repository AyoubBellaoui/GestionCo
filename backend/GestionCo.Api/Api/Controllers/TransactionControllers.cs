using GestionCo.Api.Application.Achats;
using GestionCo.Api.Application.Factures;
using GestionCo.Api.Application.Paiements;
using GestionCo.Api.Application.Ventes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCo.Api.Api.Controllers;

[ApiController]
[Route("api/ventes")]
[Authorize]
public class VentesController : ControllerBase
{
    private readonly IMediator _mediator;
    public VentesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetVentesQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetVenteByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Create([FromBody] CreateVenteDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateVenteCommand(dto), ct));

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelDto dto, CancellationToken ct)
    {
        await _mediator.Send(new CancelVenteCommand(id, dto?.Raison), ct);
        return Ok(new { message = "Vente annulée et stock restauré" });
    }
}

public class CancelDto { public string? Raison { get; set; } }

[ApiController]
[Route("api/achats")]
[Authorize(Policy = "AdminOrManager")]
public class AchatsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AchatsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAchatsQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAchatByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAchatDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateAchatCommand(dto), ct));
}

[ApiController]
[Route("api/paiements")]
[Authorize(Policy = "AdminOrManager")]
public class PaiementsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaiementsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetPaiementsQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaiementDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreatePaiementCommand(dto), ct));
}

[ApiController]
[Route("api/factures")]
[Authorize]
public class FacturesController : ControllerBase
{
    private readonly IMediator _mediator;
    public FacturesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetFacturesQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetFactureByIdQuery(id), ct));

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken ct)
    {
        var facture = await _mediator.Send(new GetFactureByIdQuery(id), ct);
        var bytes = await _mediator.Send(new DownloadFacturePdfQuery(id), ct);
        var fileName = $"Facture-{facture.NumeroFacture}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Create([FromBody] CreateFactureDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateFactureCommand(dto), ct));

    [HttpGet("ventes-sans-facture")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> GetVentesSansFacture(CancellationToken ct)
        => Ok(await _mediator.Send(new GetVentesSansFactureQuery(), ct));
}
