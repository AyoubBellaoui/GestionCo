using GestionCo.Api.Application.Achats;
using GestionCo.Api.Application.Charges;
using GestionCo.Api.Application.Devis;
using GestionCo.Api.Application.Factures;
using GestionCo.Api.Application.Notifications;
using GestionCo.Api.Application.Paiements;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Reports;
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

    [HttpPost("{id}/paiements")]
    public async Task<IActionResult> AddPaiement(int id, [FromBody] AddPaiementVenteDto dto, CancellationToken ct)
    {
        dto.VenteId = id;
        return Ok(await _mediator.Send(new AddPaiementVenteCommand(dto), ct));
    }

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

    [HttpPost("{id}/paiements")]
    public async Task<IActionResult> AddPaiement(int id, [FromBody] AddPaiementAchatDto dto, CancellationToken ct)
    {
        dto.AchatId = id;
        return Ok(await _mediator.Send(new AddPaiementAchatCommand(dto), ct));
    }

    [HttpGet("paiements")]
    public async Task<IActionResult> GetPaiements([FromQuery] GetPaiementsAchatQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));
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

    [HttpPost("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id, [FromBody] EntrepriseInfoDto? info, CancellationToken ct)
    {
        var facture = await _mediator.Send(new GetFactureByIdQuery(id), ct);
        var bytes = await _mediator.Send(new DownloadFacturePdfQuery(id, info), ct);
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

[ApiController]
[Route("api/charges")]
[Authorize(Policy = "AdminOrManager")]
public class ChargesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ChargesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetChargesQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetChargeByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChargeDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateChargeCommand(dto), ct));

    [HttpPost("{id}/paiements")]
    public async Task<IActionResult> AddPaiement(int id, [FromBody] AddPaiementChargeDto dto, CancellationToken ct)
    {
        dto.ChargeId = id;
        return Ok(await _mediator.Send(new AddPaiementChargeCommand(dto), ct));
    }

    [HttpGet("paiements")]
    public async Task<IActionResult> GetPaiements([FromQuery] GetPaiementsChargeQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));
}

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int limit = 30, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetNotificationsQuery(limit), ct));

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await _mediator.Send(new MarkNotificationReadCommand(id), ct);
        return Ok(new { message = "Notification marquée comme lue" });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return Ok(new { message = "Toutes les notifications marquées comme lues" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteNotificationCommand(id), ct);
        return Ok(new { message = "Notification supprimée" });
    }

    [HttpDelete("read")]
    public async Task<IActionResult> DeleteAllRead(CancellationToken ct)
    {
        await _mediator.Send(new DeleteAllReadNotificationsCommand(), ct);
        return Ok(new { message = "Notifications lues supprimées" });
    }
}

[ApiController]
[Route("api/categories-charge")]
[Authorize(Policy = "AdminOrManager")]
public class CategoriesChargeController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesChargeController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCategoriesChargeQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategorieChargeDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCategorieChargeCommand(dto), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCategorieChargeCommand(id), ct);
        return Ok(new { message = "Catégorie supprimée" });
    }
}

[ApiController]
[Route("api/devis")]
[Authorize(Policy = "AdminOrManager")]
public class DevisController : ControllerBase
{
    private readonly IMediator _mediator;
    public DevisController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetDevisQuery q, CancellationToken ct)
        => Ok(await _mediator.Send(q, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetDevisByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDevisDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateDevisCommand(dto), ct));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDevisDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateDevisCommand(id, dto), ct));

    [HttpPut("{id}/statut")]
    public async Task<IActionResult> UpdateStatut(int id, [FromBody] UpdateDevisStatutDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateDevisStatutCommand(id, dto.Statut), ct));

    [HttpPost("{id}/convertir")]
    public async Task<IActionResult> Convertir(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new ConvertirDevisEnVenteCommand(id), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDevisCommand(id), ct);
        return Ok(new { message = "Devis supprimé" });
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController(IMediator mediator, IPdfService pdf, IConfiguration config) : ControllerBase
{
    private string EntrepriseName => config["EntrepriseInfo:RaisonSociale"] ?? "GestionCo.";

    [HttpGet("pl/{annee:int}")]
    public async Task<IActionResult> GetPL(int annee, CancellationToken ct)
        => Ok(await mediator.Send(new GetPLReportQuery(annee), ct));

    [HttpGet("tva/{annee:int}")]
    public async Task<IActionResult> GetTVA(int annee, CancellationToken ct)
        => Ok(await mediator.Send(new GetTVAReportQuery(annee), ct));

    [HttpGet("pl/{annee:int}/pdf")]
    public async Task<IActionResult> GetPLPdf(int annee, CancellationToken ct)
    {
        var report = await mediator.Send(new GetPLReportQuery(annee), ct);
        var bytes = pdf.GeneratePLReportPdf(report, EntrepriseName);
        return File(bytes, "application/pdf", $"rapport-pl-{annee}.pdf");
    }

    [HttpGet("tva/{annee:int}/pdf")]
    public async Task<IActionResult> GetTVAPdf(int annee, CancellationToken ct)
    {
        var report = await mediator.Send(new GetTVAReportQuery(annee), ct);
        var bytes = pdf.GenerateTVAReportPdf(report, EntrepriseName);
        return File(bytes, "application/pdf", $"rapport-tva-{annee}.pdf");
    }
}
