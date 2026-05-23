using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandeModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commandes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    DevisId = table.Column<int>(type: "int", nullable: true),
                    UtilisateurId = table.Column<int>(type: "int", nullable: false),
                    DateCommande = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLivraison = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MontantTotalHT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontantTVA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontantTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VenteId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commandes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commandes_clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commandes_devis_DevisId",
                        column: x => x.DevisId,
                        principalTable: "devis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_commandes_utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commandes_ventes_VenteId",
                        column: x => x.VenteId,
                        principalTable: "ventes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "lignes_commande",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandeId = table.Column<int>(type: "int", nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remise = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Tva = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lignes_commande", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lignes_commande_commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lignes_commande_produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commandes_ClientId",
                table: "commandes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_DevisId",
                table: "commandes",
                column: "DevisId");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_Reference",
                table: "commandes",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commandes_UtilisateurId",
                table: "commandes",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_VenteId",
                table: "commandes",
                column: "VenteId");

            migrationBuilder.CreateIndex(
                name: "IX_lignes_commande_CommandeId",
                table: "lignes_commande",
                column: "CommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_lignes_commande_ProduitId",
                table: "lignes_commande",
                column: "ProduitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "lignes_commande");
            migrationBuilder.DropTable(name: "commandes");
        }
    }
}
