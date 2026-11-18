using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TpMiercoles.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDistanciaYDuracionARuta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DistanciaMetros",
                table: "Rutas",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DuracionSegundos",
                table: "Rutas",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanciaMetros",
                table: "Rutas");

            migrationBuilder.DropColumn(
                name: "DuracionSegundos",
                table: "Rutas");
        }
    }
}
