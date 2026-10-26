using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TpMiercoles.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class AgregaciondehoraSalidayHorallegadaenrutayordenenparadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "HoraLlegada",
                table: "Ruta",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HoraSalida",
                table: "Ruta",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "Parada",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoraLlegada",
                table: "Ruta");

            migrationBuilder.DropColumn(
                name: "HoraSalida",
                table: "Ruta");

            migrationBuilder.DropColumn(
                name: "Orden",
                table: "Parada");
        }
    }
}
