using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEtatLivraison : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EtatLivraison",
                table: "commandes",
                type: "int",
                nullable: false,
                defaultValue: 1); // 1 = NonCommence

            // Migrate old EnCours (3) → Confirmee (2) + EnPreparation (2)
            migrationBuilder.Sql("UPDATE commandes SET Statut = 2, EtatLivraison = 2 WHERE Statut = 3");

            // Migrate old Livree (4) → Confirmee (2) + Livre (4)
            migrationBuilder.Sql("UPDATE commandes SET Statut = 2, EtatLivraison = 4 WHERE Statut = 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EtatLivraison",
                table: "commandes");
        }
    }
}
