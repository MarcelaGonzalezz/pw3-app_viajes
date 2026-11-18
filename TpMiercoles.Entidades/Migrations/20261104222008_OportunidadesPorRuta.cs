using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TpMiercoles.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class OportunidadesPorRuta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parada_Rutas_RutaId",
                table: "Parada");

            migrationBuilder.AlterColumn<int>(
                name: "RutaId",
                table: "Parada",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ViajeId",
                table: "Parada",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parada_ViajeId",
                table: "Parada",
                column: "ViajeId");

            // Copia las paradas de la ruta a los viajes que ya existían, para que el
            // detalle de esos viajes siga mostrando el recorrido (snapshot).
            migrationBuilder.Sql("""
                INSERT INTO Parada (RutaId, ViajeId, DireccionId, Orden)
                SELECT NULL, v.Id, p.DireccionId, p.Orden
                FROM Viaje v
                INNER JOIN Parada p ON p.RutaId = v.RutaId
                WHERE p.ViajeId IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Parada_UnDuenio",
                table: "Parada",
                sql: "([RutaId] IS NOT NULL AND [ViajeId] IS NULL) OR ([RutaId] IS NULL AND [ViajeId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Parada_Rutas_RutaId",
                table: "Parada",
                column: "RutaId",
                principalTable: "Rutas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Parada_Viaje_ViajeId",
                table: "Parada",
                column: "ViajeId",
                principalTable: "Viaje",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parada_Rutas_RutaId",
                table: "Parada");

            migrationBuilder.DropForeignKey(
                name: "FK_Parada_Viaje_ViajeId",
                table: "Parada");

            migrationBuilder.DropIndex(
                name: "IX_Parada_ViajeId",
                table: "Parada");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Parada_UnDuenio",
                table: "Parada");

            migrationBuilder.Sql("DELETE FROM Parada WHERE ViajeId IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "ViajeId",
                table: "Parada");

            migrationBuilder.AlterColumn<int>(
                name: "RutaId",
                table: "Parada",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Parada_Rutas_RutaId",
                table: "Parada",
                column: "RutaId",
                principalTable: "Rutas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
