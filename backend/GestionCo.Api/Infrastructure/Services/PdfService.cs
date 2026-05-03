using GestionCo.Api.Application.Common.Interfaces;
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

    public async Task<byte[]> GenerateInvoicePdfAsync(int factureId, CancellationToken ct = default)
    {
        var facture = await _db.Factures
            .Include(f => f.Vente).ThenInclude(v => v.Client)
            .Include(f => f.Vente).ThenInclude(v => v.Lignes).ThenInclude(l => l.Produit)
            .FirstOrDefaultAsync(f => f.Id == factureId, ct)
            ?? throw new InvalidOperationException($"Facture {factureId} non trouvée");

        var e = _config.GetSection("EntrepriseInfo");
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
                               left.Item().Text(e["RaisonSociale"] ?? "GestionCo.")
                                   .FontSize(20).Bold().FontColor(primary);
                               left.Item().PaddingTop(2)
                                   .Text(e["Adresse"] ?? "")
                                   .FontSize(9).FontColor(muted);
                               left.Item()
                                   .Text($"{e["Telephone"]} · {e["Email"]}")
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
                               c.Item().Text(e["RaisonSociale"] ?? "").Bold().FontSize(11);
                               c.Item().PaddingTop(2).Text(e["Adresse"] ?? "").FontColor("#374151");
                               c.Item().Text($"ICE : {e["ICE"]}").FontColor("#374151");
                               c.Item().Text($"RC : {e["RC"]}  ·  IF : {e["IF"]}").FontColor("#374151");
                               c.Item().Text($"Patente : {e["Patente"]}  ·  CNSS : {e["CNSS"]}").FontColor("#374151");
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
                               $"Banque : {e["Banque"]}  ·  RIB : {e["RIB"]}  ·  SWIFT : {e["Swift"]}");
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
                            .Text($"{e["RaisonSociale"]} · Capital : {e["CapitalSocial"]} · RC {e["RC"]} · ICE {e["ICE"]}")
                            .FontSize(8).FontColor(muted);
                        footer.Item().AlignCenter().PaddingTop(2)
                            .Text("Ce document est conforme à l'article 145 du Code Général des Impôts (CGI) du Maroc.")
                            .FontSize(8).FontColor(muted);
                    });
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
