using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParametresSmtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parametres_smtp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Host = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnableSsl = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parametres_smtp", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parametres_smtp");
        }
    }
}
