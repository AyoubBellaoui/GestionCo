using GestionCo.Api.Application.Dashboard;
using GestionCo.Api.Application.Logs;
using GestionCo.Api.Application.MouvementsStock;
using GestionCo.Api.Application.Utilisateurs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCo.Api.Api.Controllers;

[ApiController]
[Route("api/mouvements-stock")]
[Authorize(Policy = "AdminOrManager")]
public class MouvementsStockController : ControllerBase
{
    private readonly IMediator _mediator;
    public MouvementsStockController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetMouvementsStockQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpPost("ajustement")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateAjustement([FromBody] AjustementStockDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateAjustementStockCommand(dto), ct));
}

[ApiController]
[Route("api/logs")]
[Authorize(Policy = "AdminOnly")]
public class LogsController : ControllerBase
{
    private readonly IMediator _mediator;
    public LogsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetLogsQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => Ok(await _mediator.Send(new GetLogsStatsQuery(), ct));
}

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "AdminOrManager")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => Ok(await _mediator.Send(new GetDashboardStatsQuery(), ct));

    [HttpGet("chart")]
    public async Task<IActionResult> GetChart([FromQuery] int jours = 30, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDashboardChartQuery(jours), ct));

    [HttpGet("top-produits")]
    public async Task<IActionResult> GetTopProduits([FromQuery] int take = 5, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetTopProduitsQuery(take), ct));

    [HttpGet("stock-alertes")]
    public async Task<IActionResult> GetStockAlertes([FromQuery] int take = 10, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStockAlertesQuery(take), ct));

    [HttpGet("dernieres-ventes")]
    public async Task<IActionResult> GetDernieresVentes([FromQuery] int take = 10, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDernieresVentesQuery(take), ct));
}

[ApiController]
[Route("api/utilisateurs")]
[Authorize(Policy = "AdminOnly")]
public class UtilisateursController : ControllerBase
{
    private readonly IMediator _mediator;
    public UtilisateursController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetUtilisateursQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUtilisateurDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateUtilisateurCommand(dto), ct));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUtilisateurDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateUtilisateurCommand(id, dto), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteUtilisateurCommand(id), ct);
        return NoContent();
    }
}
