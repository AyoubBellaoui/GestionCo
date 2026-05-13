using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Reports;
using GestionCo.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionCo.Api.Infrastructure.Services;

public class PdfService : IPdfService
{
    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;

    public PdfService(IAppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private EntrepriseInfoDto InfoFromConfig() => new()
    {
        RaisonSociale = _config["EntrepriseInfo:RaisonSociale"] ?? "GestionCo. SARL",
        Adresse       = _config["EntrepriseInfo:Adresse"]       ?? "",
        Telephone     = _config["EntrepriseInfo:Telephone"]     ?? "",
        Email         = _config["EntrepriseInfo:Email"]         ?? "",
        Ice           = _config["EntrepriseInfo:ICE"]           ?? "",
        Rc            = _config["EntrepriseInfo:RC"]            ?? "",
        If            = _config["EntrepriseInfo:IF"]            ?? "",
        Patente       = _config["EntrepriseInfo:Patente"]       ?? "",
        Cnss          = _config["EntrepriseInfo:CNSS"]          ?? "",
        Capital       = _config["EntrepriseInfo:CapitalSocial"] ?? "",
        Rib           = _config["EntrepriseInfo:RIB"]           ?? "",
        Banque        = _config["EntrepriseInfo:Banque"]        ?? "",
        Swift         = _config["EntrepriseInfo:Swift"]         ?? "",
    };

    public async Task<byte[]> GenerateInvoicePdfAsync(int factureId, EntrepriseInfoDto? info = null, CancellationToken ct = default)
    {
        var facture = await _db.Factures
            .Include(f => f.Vente).ThenInclude(v => v.Client)
            .Include(f => f.Vente).ThenInclude(v => v.Lignes).ThenInclude(l => l.Produit)
            .FirstOrDefaultAsync(f => f.Id == factureId, ct)
            ?? throw new InvalidOperationException($"Facture {factureId} non trouvée");

        var e = info ?? InfoFromConfig();
        var vente = facture.Vente;
        var client = vente.Client;

        var factureStatus = facture.Statut;
        if (factureStatus == StatutFacture.EnAttente && vente != null && vente.MontantPaye > 0 && vente.MontantPaye < vente.MontantTotal)
        {
            factureStatus = StatutFacture.PartiellementPayee;
        }

        var (statutText, statutColor) = factureStatus switch
        {
            StatutFacture.Payee            => ("PAYÉE",      "#22c55e"),
            StatutFacture.PartiellementPayee => ("PARTIEL",    "#4F46E5"),
            StatutFacture.EnRetard         => ("EN RETARD",  "#ef4444"),
            StatutFacture.Annulee          => ("ANNULÉE",    "#6b7280"),
            _                              => ("EN ATTENTE", "#f59e0b"),
        };

        const string primary = "#4F46E5";
        const string muted   = "#6b7280";
        const string border  = "#e2e4ee";
        const string bg      = "#f5f6fa";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // ══════════════════════════════════════════
                // CONTENT
                // ══════════════════════════════════════════
                page.Content().Column(col =>
                {
                    // ── HEADER ──────────────────────────────
                    col.Item()
                       .BorderBottom(3).BorderColor(primary)
                       .PaddingBottom(12)
                       .Row(row =>
                       {
                           row.RelativeItem().Column(left =>
                           {
                               left.Item().Row(logoRow =>
                               {
                                   if (!string.IsNullOrEmpty(e.Logo))
                                   {
                                       try
                                       {
                                           var b64 = e.Logo.Contains(',') ? e.Logo.Split(',')[1] : e.Logo;
                                           var logoBytes = Convert.FromBase64String(b64);
                                           logoRow.AutoItem().Width(36).Height(36)
                                               .Image(logoBytes).FitArea();
                                           logoRow.AutoItem().Width(10);
                                       }
                                       catch { /* ignore bad base64 */ }
                                   }
                                   logoRow.RelativeItem().AlignMiddle()
                                       .Text(e.RaisonSociale ?? "GestionCo.")
                                       .FontSize(20).Bold().FontColor(primary);
                               });
                               left.Item().PaddingTop(2)
                                   .Text(e.Adresse ?? "")
                                   .FontSize(9).FontColor(muted);
                               left.Item()
                                   .Text($"{e.Telephone} · {e.Email}")
                                   .FontSize(9).FontColor(muted);
                           });

                           row.RelativeItem().AlignRight().Column(right =>
                           {
                               right.Item().AlignRight()
                                    .Text("FACTURE").FontSize(26).Bold().FontColor(primary);
                               right.Item().AlignRight().PaddingTop(2)
                                    .Text(facture.NumeroFacture).FontSize(13);
                               right.Item().AlignRight().PaddingTop(6).Row(r =>
                               {
                                   r.RelativeItem();
                                   r.AutoItem()
                                    .Background(statutColor)
                                    .PaddingHorizontal(10).PaddingVertical(4)
                                    .Text(statutText).FontSize(9).Bold().FontColor("#ffffff");
                               });
                           });
                       });

                    col.Item().Height(14);

                    // ── BLOCS ÉMETTEUR / CLIENT ─────────────
                    col.Item().Row(row =>
                    {
                        // Émetteur
                        row.RelativeItem()
                           .Border(1).BorderColor(border)
                           .Padding(12)
                           .Column(c =>
                           {
                               c.Item().Text("ÉMETTEUR").FontSize(8).Bold().FontColor(muted);
                               c.Item().Height(5);
                               c.Item().Text(e.RaisonSociale ?? "").Bold().FontSize(11);
                               c.Item().PaddingTop(2).Text(e.Adresse ?? "").FontColor("#374151");
                               c.Item().Text($"ICE : {e.Ice}").FontColor("#374151");
                               c.Item().Text($"RC : {e.Rc}  ·  IF : {e.If}").FontColor("#374151");
                               c.Item().Text($"Patente : {e.Patente}  ·  CNSS : {e.Cnss}").FontColor("#374151");
                           });

                        row.ConstantItem(12);

                        // Client
                        row.RelativeItem()
                           .Background("#eef0ff")
                           .Border(1).BorderColor("#c7d2fe")
                           .Padding(12)
                           .Column(c =>
                           {
                               c.Item().Text("FACTURÉ À").FontSize(8).Bold().FontColor(primary);
                               c.Item().Height(5);
                               c.Item().Text(client.NomClient).Bold().FontSize(11);
                               if (!string.IsNullOrEmpty(client.Adresse))
                                   c.Item().PaddingTop(2).Text(client.Adresse).FontColor("#374151");
                               if (!string.IsNullOrEmpty(client.ICE))
                                   c.Item().Text($"ICE : {client.ICE}").FontColor("#374151");
                               if (!string.IsNullOrEmpty(client.Telephone))
                                   c.Item().Text($"Tél : {client.Telephone}").FontColor("#374151");
                               if (!string.IsNullOrEmpty(client.Email))
                                   c.Item().Text($"Email : {client.Email}").FontColor("#374151");
                           });
                    });

                    col.Item().Height(12);

                    // ── META ────────────────────────────────
                    col.Item().Background(bg).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Date d'émission").FontSize(8).FontColor(muted);
                            c.Item().PaddingTop(2).Text(facture.DateEmission.ToString("dd MMM yyyy")).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Date d'échéance").FontSize(8).FontColor(muted);
                            c.Item().PaddingTop(2).Text(facture.DateEcheance.ToString("dd MMM yyyy")).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Réf. vente").FontSize(8).FontColor(muted);
                            c.Item().PaddingTop(2).Text(vente.Reference).Bold();
                        });
                    });

                    col.Item().Height(12);

                    // ── TABLE DES LIGNES ────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);   // Désignation
                            cols.ConstantColumn(40);  // Qté
                            cols.ConstantColumn(75);  // P.U. HT
                            cols.ConstantColumn(45);  // TVA
                            cols.ConstantColumn(75);  // Total HT
                        });

                        // En-tête
                        table.Header(h =>
                        {
                            static IContainer HeaderStyle(IContainer c)
                                => c.Background("#4F46E5").Padding(9).AlignMiddle();

                            HeaderStyle(h.Cell()).AlignLeft()
                                .Text("Désignation").FontSize(10).Bold().FontColor("#ffffff");
                            HeaderStyle(h.Cell()).AlignCenter()
                                .Text("Qté").FontSize(10).Bold().FontColor("#ffffff");
                            HeaderStyle(h.Cell()).AlignRight()
                                .Text("P.U. HT").FontSize(10).Bold().FontColor("#ffffff");
                            HeaderStyle(h.Cell()).AlignCenter()
                                .Text("TVA").FontSize(10).Bold().FontColor("#ffffff");
                            HeaderStyle(h.Cell()).AlignRight()
                                .Text("Total HT").FontSize(10).Bold().FontColor("#ffffff");
                        });

                        // Lignes
                        var idx = 0;
                        foreach (var ligne in vente.Lignes)
                        {
                            var rowBg = idx++ % 2 == 0 ? "#ffffff" : "#f9fafb";

                            static IContainer CellStyle(IContainer c, string rowBg)
                                => c.Background(rowBg).BorderBottom(1).BorderColor("#e2e4ee")
                                    .Padding(9).AlignMiddle();

                            CellStyle(table.Cell(), rowBg).AlignLeft().Column(c =>
                            {
                                c.Item().Text(ligne.Produit?.Nom ?? "").Bold();
                                c.Item().Text($"Réf. {ligne.Produit?.Reference}").FontSize(8).FontColor(muted);
                            });
                            CellStyle(table.Cell(), rowBg).AlignCenter()
                                .Text(ligne.Quantite.ToString());
                            CellStyle(table.Cell(), rowBg).AlignRight()
                                .Text($"{ligne.PrixUnitaire:N2}");
                            CellStyle(table.Cell(), rowBg).AlignCenter()
                                .Text($"{ligne.TVA:0}%");
                            CellStyle(table.Cell(), rowBg).AlignRight()
                                .Text($"{ligne.Total:N2}").Bold();
                        }
                    });

                    col.Item().Height(12);

                    // ── TOTAUX ──────────────────────────────
                    col.Item().Row(outer =>
                    {
                        outer.RelativeItem(); // spacer
                        outer.ConstantItem(265).Background(bg).Padding(14).Column(totaux =>
                        {
                            void TRow(string label, string val, bool highlight = false)
                            {
                                totaux.Item().Row(r =>
                                {
                                    var labelStyle = r.RelativeItem().Text(label)
                                        .FontSize(highlight ? 12 : 10);
                                    if (highlight) labelStyle.Bold();
                                    r.ConstantItem(110).AlignRight()
                                        .Text(val)
                                        .FontSize(highlight ? 12 : 10)
                                        .Bold()
                                        .FontColor(highlight ? primary : "#1a1d2e");
                                });
                                if (!highlight) totaux.Item().Height(5);
                            }

                            TRow("Sous-total HT", $"{vente.MontantTotalHT:N2} MAD");
                            TRow("TVA", $"{vente.MontantTVA:N2} MAD");
                            TRow("Total TTC", $"{vente.MontantTotal:N2} MAD");

                            if (vente.MontantPaye > 0)
                                TRow("Déjà payé", $"- {vente.MontantPaye:N2} MAD");

                            totaux.Item().PaddingTop(6).BorderTop(2).BorderColor(primary).Height(1);
                            totaux.Item().Height(6);
                            TRow("RESTE DÛ", $"{vente.Reste:N2} MAD", highlight: true);
                        });
                    });

                    col.Item().Height(16);

                    // ── ARRÊTÉ EN LETTRES ───────────────────
                    col.Item()
                       .Background("#eef0ff")
                       .BorderLeft(3).BorderColor(primary)
                       .Padding(12)
                       .Column(c =>
                       {
                           c.Item().Text("Arrêtée la présente facture à la somme de :")
                               .FontSize(8).Bold().FontColor(primary);
                           c.Item().Height(4);
                           c.Item().Text($"{MontantEnLettres(vente.MontantTotal)} ({vente.MontantTotal:N2} MAD TTC)");
                       });

                    col.Item().Height(12);

                    // ── COORDONNÉES BANCAIRES ───────────────
                    col.Item()
                       .Border(1).BorderColor(border)
                       .Padding(12)
                       .Column(c =>
                       {
                           c.Item().Text("COORDONNÉES BANCAIRES").FontSize(8).Bold().FontColor(muted);
                           c.Item().Height(4);
                           c.Item().Text(
                               $"Banque : {e.Banque}  ·  RIB : {e.Rib}  ·  SWIFT : {e.Swift}");
                       });
                });

                // ══════════════════════════════════════════
                // FOOTER
                // ══════════════════════════════════════════
                page.Footer()
                    .BorderTop(1).BorderColor(border)
                    .PaddingTop(8)
                    .Column(footer =>
                    {
                        footer.Item().AlignCenter()
                            .Text("Paiement sous 30 jours. Toute facture impayée fera l'objet de pénalités de retard (loi 69-00).")
                            .FontSize(8).FontColor(muted);
                        footer.Item().AlignCenter().PaddingTop(2)
                            .Text($"{e.RaisonSociale} · Capital : {e.Capital} · RC {e.Rc} · ICE {e.Ice}")
                            .FontSize(8).FontColor(muted);
                        footer.Item().AlignCenter().PaddingTop(2)
                            .Text("Ce document est conforme à l'article 145 du Code Général des Impôts (CGI) du Maroc.")
                            .FontSize(8).FontColor(muted);
                    });
            });
        }).GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════
    // DEVIS PDF
    // ═══════════════════════════════════════════════════════
    public async Task<byte[]> GenerateDevisPdfAsync(int devisId, EntrepriseInfoDto? info = null, CancellationToken ct = default)
    {
        var devis = await _db.Devis
            .Include(d => d.Client)
            .Include(d => d.Lignes).ThenInclude(l => l.Produit)
            .FirstOrDefaultAsync(d => d.Id == devisId, ct)
            ?? throw new InvalidOperationException($"Devis {devisId} non trouvé");

        var e = info ?? InfoFromConfig();
        var client = devis.Client;

        const string primary = "#4F46E5";
        const string muted   = "#6b7280";
        const string border  = "#e2e4ee";
        const string bg      = "#f5f6fa";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── HEADER ──────────────────────────────
                    col.Item()
                       .BorderBottom(3).BorderColor(primary)
                       .PaddingBottom(12)
                       .Row(row =>
                       {
                           row.RelativeItem().Column(left =>
                           {
                               left.Item().Row(logoRow =>
                               {
                                   if (!string.IsNullOrEmpty(e.Logo))
                                   {
                                       try
                                       {
                                           var b64 = e.Logo.Contains(',') ? e.Logo.Split(',')[1] : e.Logo;
                                           var logoBytes = Convert.FromBase64String(b64);
                                           logoRow.AutoItem().Width(36).Height(36).Image(logoBytes).FitArea();
                                           logoRow.AutoItem().Width(10);
                                       }
                                       catch { }
                                   }
                                   logoRow.RelativeItem().AlignMiddle()
                                       .Text(e.RaisonSociale ?? "GestionCo.")
                                       .FontSize(20).Bold().FontColor(primary);
                               });
                               left.Item().PaddingTop(2).Text(e.Adresse ?? "").FontSize(9).FontColor(muted);
                               left.Item().Text($"{e.Telephone} · {e.Email}").FontSize(9).FontColor(muted);
                           });

                           row.RelativeItem().AlignRight().Column(right =>
                           {
                               right.Item().AlignRight().Text("DEVIS").FontSize(26).Bold().FontColor(primary);
                               right.Item().AlignRight().PaddingTop(2).Text(devis.Reference).FontSize(13);
                               right.Item().AlignRight().PaddingTop(6).Row(r =>
                               {
                                   r.RelativeItem();
                                   var (sText, sColor) = devis.Statut switch
                                   {
                                       StatutDevis.Accepte  => ("ACCEPTÉ",  "#22c55e"),
                                       StatutDevis.Refuse   => ("REFUSÉ",   "#ef4444"),
                                       StatutDevis.Converti => ("CONVERTI", "#8b5cf6"),
                                       StatutDevis.Envoye   => ("ENVOYÉ",   "#3b82f6"),
                                       StatutDevis.Expire   => ("EXPIRÉ",   "#f59e0b"),
                                       _                    => ("BROUILLON","#6b7280"),
                                   };
                                   r.AutoItem().Background(sColor).PaddingHorizontal(10).PaddingVertical(4)
                                    .Text(sText).FontSize(9).Bold().FontColor("#ffffff");
                               });
                           });
                       });

                    col.Item().Height(14);

                    // ── BLOCS ÉMETTEUR / CLIENT ─────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(border).Padding(12).Column(c =>
                        {
                            c.Item().Text("ÉMETTEUR").FontSize(8).Bold().FontColor(muted);
                            c.Item().Height(5);
                            c.Item().Text(e.RaisonSociale ?? "").Bold().FontSize(11);
                            c.Item().PaddingTop(2).Text(e.Adresse ?? "").FontColor("#374151");
                            c.Item().Text($"ICE : {e.Ice}").FontColor("#374151");
                            c.Item().Text($"RC : {e.Rc}  ·  IF : {e.If}").FontColor("#374151");
                        });

                        row.ConstantItem(12);

                        row.RelativeItem().Background("#eef0ff").Border(1).BorderColor("#c7d2fe").Padding(12).Column(c =>
                        {
                            c.Item().Text("DESTINATAIRE").FontSize(8).Bold().FontColor(primary);
                            c.Item().Height(5);
                            c.Item().Text(client.NomClient).Bold().FontSize(11);
                            if (!string.IsNullOrEmpty(client.Adresse))
                                c.Item().PaddingTop(2).Text(client.Adresse).FontColor("#374151");
                            if (!string.IsNullOrEmpty(client.ICE))
                                c.Item().Text($"ICE : {client.ICE}").FontColor("#374151");
                            if (!string.IsNullOrEmpty(client.Telephone))
                                c.Item().Text($"Tél : {client.Telephone}").FontColor("#374151");
                            if (!string.IsNullOrEmpty(client.Email))
                                c.Item().Text($"Email : {client.Email}").FontColor("#374151");
                        });
                    });

                    col.Item().Height(12);

                    // ── META ────────────────────────────────
                    col.Item().Background(bg).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Date du devis").FontSize(8).FontColor(muted);
                            c.Item().PaddingTop(2).Text(devis.DateDevis.ToString("dd MMM yyyy")).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Validité").FontSize(8).FontColor(muted);
                            c.Item().PaddingTop(2).Text(
                                devis.DateValidite.HasValue ? devis.DateValidite.Value.ToString("dd MMM yyyy") : "Illimitée"
                            ).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Référence").FontSize(8).FontColor(muted);
                            c.Item().PaddingTop(2).Text(devis.Reference).Bold();
                        });
                    });

                    col.Item().Height(12);

                    // ── TABLE DES LIGNES ────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(40);
                            cols.ConstantColumn(75);
                            cols.ConstantColumn(45);
                            cols.ConstantColumn(75);
                        });

                        table.Header(h =>
                        {
                            static IContainer HeaderStyle(IContainer c)
                                => c.Background("#4F46E5").Padding(9).AlignMiddle();

                            HeaderStyle(h.Cell()).AlignLeft().Text("Désignation").FontSize(10).Bold().FontColor("#ffffff");
                            HeaderStyle(h.Cell()).AlignCenter().Text("Qté").FontSize(10).Bold().FontColor("#ffffff");
                            HeaderStyle(h.Cell()).AlignRight().Text("P.U. HT").FontSize(10).Bold().FontColor("#ffffff");
                            HeaderStyle(h.Cell()).AlignCenter().Text("TVA").FontSize(10).Bold().FontColor("#ffffff");
                            HeaderStyle(h.Cell()).AlignRight().Text("Total HT").FontSize(10).Bold().FontColor("#ffffff");
                        });

                        var idx = 0;
                        foreach (var ligne in devis.Lignes)
                        {
                            var rowBg = idx++ % 2 == 0 ? "#ffffff" : "#f9fafb";

                            static IContainer CellStyle(IContainer c, string bg)
                                => c.Background(bg).BorderBottom(1).BorderColor("#e2e4ee").Padding(9).AlignMiddle();

                            CellStyle(table.Cell(), rowBg).AlignLeft().Column(c =>
                            {
                                c.Item().Text(ligne.Produit?.Nom ?? "").Bold();
                                c.Item().Text($"Réf. {ligne.Produit?.Reference}").FontSize(8).FontColor(muted);
                            });
                            CellStyle(table.Cell(), rowBg).AlignCenter().Text(ligne.Quantite.ToString());
                            CellStyle(table.Cell(), rowBg).AlignRight().Text($"{ligne.PrixUnitaire:N2}");
                            CellStyle(table.Cell(), rowBg).AlignCenter().Text($"{ligne.Tva:0}%");
                            CellStyle(table.Cell(), rowBg).AlignRight().Text($"{ligne.Total:N2}").Bold();
                        }
                    });

                    col.Item().Height(12);

                    // ── TOTAUX ──────────────────────────────
                    col.Item().Row(outer =>
                    {
                        outer.RelativeItem();
                        outer.ConstantItem(265).Background(bg).Padding(14).Column(totaux =>
                        {
                            void TRow(string label, string val, bool highlight = false)
                            {
                                totaux.Item().Row(r =>
                                {
                                    var ls = r.RelativeItem().Text(label).FontSize(highlight ? 12 : 10);
                                    if (highlight) ls.Bold();
                                    r.ConstantItem(110).AlignRight().Text(val)
                                        .FontSize(highlight ? 12 : 10).Bold()
                                        .FontColor(highlight ? primary : "#1a1d2e");
                                });
                                if (!highlight) totaux.Item().Height(5);
                            }
                            TRow("Sous-total HT", $"{devis.MontantTotalHT:N2} MAD");
                            TRow("TVA", $"{devis.MontantTVA:N2} MAD");
                            totaux.Item().PaddingTop(6).BorderTop(2).BorderColor(primary).Height(1);
                            totaux.Item().Height(6);
                            TRow("TOTAL TTC", $"{devis.MontantTotal:N2} MAD", highlight: true);
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(devis.Notes))
                    {
                        col.Item().Height(12);
                        col.Item().Background("#eef0ff").BorderLeft(3).BorderColor(primary).Padding(12).Column(c =>
                        {
                            c.Item().Text("Notes").FontSize(8).Bold().FontColor(primary);
                            c.Item().Height(4);
                            c.Item().Text(devis.Notes);
                        });
                    }
                });

                page.Footer()
                    .BorderTop(1).BorderColor(border).PaddingTop(8)
                    .Column(footer =>
                    {
                        footer.Item().AlignCenter()
                            .Text("Ce devis est valable jusqu'à la date de validité indiquée. Merci de votre confiance.")
                            .FontSize(8).FontColor(muted);
                        footer.Item().AlignCenter().PaddingTop(2)
                            .Text($"{e.RaisonSociale} · Capital : {e.Capital} · RC {e.Rc} · ICE {e.Ice}")
                            .FontSize(8).FontColor(muted);
                    });
            });
        }).GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════
    // P&L REPORT PDF
    // ═══════════════════════════════════════════════════════
    public byte[] GeneratePLReportPdf(PLReportDto r, string entreprise)
    {
        const string primary = "#4F46E5";
        const string success = "#22c55e";
        const string danger  = "#ef4444";
        const string muted   = "#6b7280";
        const string border  = "#e2e4ee";
        const string bg      = "#f5f6fa";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().BorderBottom(3).BorderColor(primary).PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(entreprise).FontSize(16).Bold().FontColor(primary);
                            c.Item().PaddingTop(2).Text("Compte de Résultat (P&L)").FontSize(11).FontColor(muted);
                        });
                        row.AutoItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Exercice {r.Annee}").FontSize(20).Bold().FontColor(primary);
                            c.Item().AlignRight().PaddingTop(2).Text($"Généré le {DateTime.Now:dd/MM/yyyy}").FontSize(9).FontColor(muted);
                        });
                    });

                    col.Item().Height(12);

                    // Summary KPIs
                    col.Item().Background(bg).Padding(10).Row(kpis =>
                    {
                        void Kpi(string label, decimal val, string color)
                        {
                            kpis.RelativeItem().Column(c =>
                            {
                                c.Item().Text(label).FontSize(7).FontColor(muted);
                                c.Item().PaddingTop(2).Text($"{val:N2} MAD").FontSize(11).Bold().FontColor(color);
                            });
                        }
                        Kpi("CA HT",             r.TotalRevenuHT,      primary);
                        Kpi("TVA Collectée",      r.TotalRevenuTVA,     muted);
                        Kpi("Coût Achats",        r.TotalCoutAchat,     danger);
                        Kpi("Charges Opérat.",    r.TotalChargesOp,     "#f59e0b");
                        Kpi("Résultat Brut",      r.TotalResultatBrut,  r.TotalResultatBrut >= 0 ? success : danger);
                        Kpi("Résultat Net",       r.TotalResultatNet,   r.TotalResultatNet  >= 0 ? success : danger);
                    });

                    col.Item().Height(10);

                    // Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(70);   // Mois
                            cols.RelativeColumn();      // CA HT
                            cols.RelativeColumn();      // TVA Coll.
                            cols.RelativeColumn();      // Coût Achat
                            cols.RelativeColumn();      // Charges
                            cols.RelativeColumn();      // Résult. Brut
                            cols.RelativeColumn();      // Résult. Net
                        });

                        // Header row
                        table.Header(h =>
                        {
                            IContainer Hdr(IContainer c) => c.Background(primary).Padding(7).AlignMiddle();
                            string[] headers = ["Mois", "CA HT", "TVA Coll.", "Coût Achat", "Charges Op.", "Rés. Brut", "Rés. Net"];
                            foreach (var lbl in headers)
                                Hdr(h.Cell()).AlignCenter().Text(lbl).FontSize(8).Bold().FontColor("#fff");
                        });

                        // Data rows
                        int idx = 0;
                        foreach (var m in r.Mois)
                        {
                            var rowBg = idx++ % 2 == 0 ? "#ffffff" : "#f9fafb";
                            var isEmpty = m.RevenuHT == 0 && m.CoutAchat == 0;
                            IContainer Cell(IContainer c) => c.Background(rowBg).BorderBottom(1).BorderColor(border).PaddingVertical(6).PaddingHorizontal(7).AlignMiddle();

                            Cell(table.Cell()).Text(m.NomMois).Bold().FontSize(8.5f).FontColor(isEmpty ? "#9ca3af" : "#1a1d2e");
                            Cell(table.Cell()).AlignRight().Text(m.RevenuHT == 0 ? "—" : $"{m.RevenuHT:N2}").FontColor(isEmpty ? "#9ca3af" : "#1a1d2e");
                            Cell(table.Cell()).AlignRight().Text(m.RevenuTVA == 0 ? "—" : $"{m.RevenuTVA:N2}").FontColor(muted);
                            Cell(table.Cell()).AlignRight().Text(m.CoutAchat == 0 ? "—" : $"{m.CoutAchat:N2}").FontColor(danger);
                            Cell(table.Cell()).AlignRight().Text(m.ChargesOp == 0 ? "—" : $"{m.ChargesOp:N2}").FontColor("#f59e0b");
                            Cell(table.Cell()).AlignRight().Text(isEmpty ? "—" : $"{m.ResultatBrut:N2}").Bold().FontColor(m.ResultatBrut >= 0 ? success : danger);
                            Cell(table.Cell()).AlignRight().Text(isEmpty ? "—" : $"{m.ResultatNet:N2}").Bold().FontColor(m.ResultatNet >= 0 ? success : danger);
                        }

                        // Totals row
                        IContainer Tot(IContainer c) => c.Background("#1a1d2e").PaddingVertical(8).PaddingHorizontal(7).AlignMiddle();
                        Tot(table.Cell()).Text("TOTAL").FontSize(9).Bold().FontColor("#fff");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalRevenuHT:N2}").Bold().FontColor("#fff");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalRevenuTVA:N2}").FontColor("#9ca3af");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalCoutAchat:N2}").FontColor("#fca5a5");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalChargesOp:N2}").FontColor("#fcd34d");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalResultatBrut:N2}").Bold().FontColor(r.TotalResultatBrut >= 0 ? "#86efac" : "#fca5a5");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalResultatNet:N2}").Bold().FontColor(r.TotalResultatNet >= 0 ? "#86efac" : "#fca5a5");
                    });
                });

                page.Footer().BorderTop(1).BorderColor(border).PaddingTop(6)
                    .AlignCenter().Text($"{entreprise} · Rapport P&L {r.Annee} · Confidentiel")
                    .FontSize(8).FontColor(muted);
            });
        }).GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════
    // TVA REPORT PDF
    // ═══════════════════════════════════════════════════════
    public byte[] GenerateTVAReportPdf(TVAReportDto r, string entreprise)
    {
        const string primary = "#4F46E5";
        const string success = "#22c55e";
        const string danger  = "#ef4444";
        const string muted   = "#6b7280";
        const string border  = "#e2e4ee";
        const string bg      = "#f5f6fa";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().BorderBottom(3).BorderColor(primary).PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(entreprise).FontSize(16).Bold().FontColor(primary);
                            c.Item().PaddingTop(2).Text("Déclaration TVA").FontSize(11).FontColor(muted);
                        });
                        row.AutoItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Exercice {r.Annee}").FontSize(20).Bold().FontColor(primary);
                            c.Item().AlignRight().PaddingTop(2).Text($"Généré le {DateTime.Now:dd/MM/yyyy}").FontSize(9).FontColor(muted);
                        });
                    });

                    col.Item().Height(12);

                    // Summary KPIs
                    col.Item().Background(bg).Padding(12).Row(kpis =>
                    {
                        void Kpi(string label, decimal val, string color)
                        {
                            kpis.RelativeItem().Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor(muted);
                                c.Item().PaddingTop(3).Text($"{val:N2} MAD").FontSize(13).Bold().FontColor(color);
                            });
                        }
                        Kpi("TVA Collectée",  r.TotalTVACollectee,  primary);
                        Kpi("TVA Déductible", r.TotalTVADeductible, success);
                        Kpi("TVA Nette",      r.TotalTVANette,      r.TotalTVANette >= 0 ? danger : success);
                    });

                    col.Item().Height(12);

                    // Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(90);    // Mois
                            cols.RelativeColumn();       // TVA Collectée
                            cols.RelativeColumn();       // TVA Déductible
                            cols.RelativeColumn();       // TVA Nette
                            cols.ConstantColumn(100);   // Situation
                        });

                        table.Header(h =>
                        {
                            IContainer Hdr(IContainer c) => c.Background(primary).Padding(9).AlignMiddle();
                            foreach (var lbl in new[] { "Mois", "TVA Collectée", "TVA Déductible", "TVA Nette", "Situation" })
                                Hdr(h.Cell()).AlignCenter().Text(lbl).FontSize(9).Bold().FontColor("#fff");
                        });

                        int idx = 0;
                        foreach (var m in r.Mois)
                        {
                            var rowBg = idx++ % 2 == 0 ? "#ffffff" : "#f9fafb";
                            var isEmpty = m.TVACollectee == 0 && m.TVADeductible == 0;
                            IContainer Cell(IContainer c) => c.Background(rowBg).BorderBottom(1).BorderColor(border).Padding(8).AlignMiddle();
                            var situation = isEmpty ? "—" : m.TVANette > 0 ? "À reverser" : m.TVANette < 0 ? "Crédit TVA" : "Nul";
                            var sitColor = isEmpty ? muted : m.TVANette > 0 ? danger : m.TVANette < 0 ? success : muted;

                            Cell(table.Cell()).Text(m.NomMois).Bold().FontColor(isEmpty ? "#9ca3af" : "#1a1d2e");
                            Cell(table.Cell()).AlignRight().Text(isEmpty ? "—" : $"{m.TVACollectee:N2}").FontColor(primary);
                            Cell(table.Cell()).AlignRight().Text(isEmpty ? "—" : $"{m.TVADeductible:N2}").FontColor(success);
                            Cell(table.Cell()).AlignRight().Text(isEmpty ? "—" : $"{m.TVANette:N2}").Bold().FontColor(isEmpty ? muted : m.TVANette >= 0 ? danger : success);
                            Cell(table.Cell()).AlignCenter().Text(situation).Bold().FontColor(sitColor);
                        }

                        IContainer Tot(IContainer c) => c.Background("#1a1d2e").Padding(9).AlignMiddle();
                        Tot(table.Cell()).Text("TOTAL").FontSize(10).Bold().FontColor("#fff");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalTVACollectee:N2}").Bold().FontColor("#c7d2fe");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalTVADeductible:N2}").Bold().FontColor("#86efac");
                        Tot(table.Cell()).AlignRight().Text($"{r.TotalTVANette:N2}").Bold().FontColor(r.TotalTVANette >= 0 ? "#fca5a5" : "#86efac");
                        Tot(table.Cell()).AlignCenter().Text(r.TotalTVANette > 0 ? "À reverser" : r.TotalTVANette < 0 ? "Crédit TVA" : "Équilibre").Bold().FontColor(r.TotalTVANette >= 0 ? "#fca5a5" : "#86efac");
                    });

                    col.Item().Height(16);
                    col.Item().Background("#eef0ff").BorderLeft(3).BorderColor(primary).Padding(10).Column(c =>
                    {
                        c.Item().Text("Note légale").FontSize(8).Bold().FontColor(primary);
                        c.Item().Height(3);
                        c.Item().Text("TVA déductible estimée sur la base du taux TVA de chaque produit acheté. Document à valider avec votre comptable avant dépôt officiel.").FontSize(8).FontColor(muted);
                    });
                });

                page.Footer().BorderTop(1).BorderColor(border).PaddingTop(6)
                    .AlignCenter().Text($"{entreprise} · Déclaration TVA {r.Annee} · Document interne")
                    .FontSize(8).FontColor(muted);
            });
        }).GeneratePdf();
    }

    private static string MontantEnLettres(decimal montant)
    {
        var entier = (long)montant;
        var centimes = (int)((montant - entier) * 100);
        return $"{NombreEnLettres(entier)} dirhams, {centimes:00} centimes";
    }

    private static string NombreEnLettres(long n)
    {
        if (n == 0) return "zéro";
        if (n < 0) return "moins " + NombreEnLettres(-n);

        var units = new[] { "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
                            "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize",
                            "dix-sept", "dix-huit", "dix-neuf" };
        var tens  = new[] { "", "", "vingt", "trente", "quarante", "cinquante", "soixante",
                            "soixante", "quatre-vingt", "quatre-vingt" };

        string result = "";
        if (n >= 1_000_000) { var m = n / 1_000_000; result += NombreEnLettres(m) + (m == 1 ? " million " : " millions "); n %= 1_000_000; }
        if (n >= 1_000)     { var m = n / 1_000;     result += (m == 1 ? "mille " : NombreEnLettres(m) + " mille ");       n %= 1_000; }
        if (n >= 100)       { var m = n / 100;        result += (m == 1 ? "cent " : units[m] + " cent ");                   n %= 100; }
        if (n >= 20)
        {
            var t = n / 10; var u = n % 10;
            result += tens[t];
            if (t == 7 || t == 9)  result += "-" + units[10 + u];
            else if (u > 0)        result += (u == 1 ? " et " : "-") + units[u];
            else if (t == 8)       result += "s";
        }
        else if (n > 0) result += units[n];

        return result.Trim();
    }
}
