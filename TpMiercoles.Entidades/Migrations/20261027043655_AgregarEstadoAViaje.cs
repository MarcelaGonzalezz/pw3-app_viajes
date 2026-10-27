using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TpMiercoles.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoAViaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignacion_Viaje_ViajeId",
                table: "Asignacion");

            migrationBuilder.DropForeignKey(
                name: "FK_Viaje_Direccion_DireccionDestinoId",
                table: "Viaje");

            migrationBuilder.DropForeignKey(
                name: "FK_Viaje_Direccion_DireccionOrigenId",
                table: "Viaje");

            migrationBuilder.DropForeignKey(
                name: "FK_Viaje_Ruta_RutaId",
                table: "Viaje");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Viaje",
                table: "Viaje");

            migrationBuilder.RenameTable(
                name: "Viaje",
                newName: "Viajes");

            migrationBuilder.RenameIndex(
                name: "IX_Viaje_RutaId",
                table: "Viajes",
                newName: "IX_Viajes_RutaId");

            migrationBuilder.RenameIndex(
                name: "IX_Viaje_DireccionOrigenId",
                table: "Viajes",
                newName: "IX_Viajes_DireccionOrigenId");

            migrationBuilder.RenameIndex(
                name: "IX_Viaje_DireccionDestinoId",
                table: "Viajes",
                newName: "IX_Viajes_DireccionDestinoId");

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Viajes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Viajes",
                table: "Viajes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignacion_Viajes_ViajeId",
                table: "Asignacion",
                column: "ViajeId",
                principalTable: "Viajes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viajes_Direccion_DireccionDestinoId",
                table: "Viajes",
                column: "DireccionDestinoId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viajes_Direccion_DireccionOrigenId",
                table: "Viajes",
                column: "DireccionOrigenId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viajes_Ruta_RutaId",
                table: "Viajes",
                column: "RutaId",
                principalTable: "Ruta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignacion_Viajes_ViajeId",
                table: "Asignacion");

            migrationBuilder.DropForeignKey(
                name: "FK_Viajes_Direccion_DireccionDestinoId",
                table: "Viajes");

            migrationBuilder.DropForeignKey(
                name: "FK_Viajes_Direccion_DireccionOrigenId",
                table: "Viajes");

            migrationBuilder.DropForeignKey(
                name: "FK_Viajes_Ruta_RutaId",
                table: "Viajes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Viajes",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Viajes");

            migrationBuilder.RenameTable(
                name: "Viajes",
                newName: "Viaje");

            migrationBuilder.RenameIndex(
                name: "IX_Viajes_RutaId",
                table: "Viaje",
                newName: "IX_Viaje_RutaId");

            migrationBuilder.RenameIndex(
                name: "IX_Viajes_DireccionOrigenId",
                table: "Viaje",
                newName: "IX_Viaje_DireccionOrigenId");

            migrationBuilder.RenameIndex(
                name: "IX_Viajes_DireccionDestinoId",
                table: "Viaje",
                newName: "IX_Viaje_DireccionDestinoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Viaje",
                table: "Viaje",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignacion_Viaje_ViajeId",
                table: "Asignacion",
                column: "ViajeId",
                principalTable: "Viaje",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viaje_Direccion_DireccionDestinoId",
                table: "Viaje",
                column: "DireccionDestinoId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viaje_Direccion_DireccionOrigenId",
                table: "Viaje",
                column: "DireccionOrigenId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viaje_Ruta_RutaId",
                table: "Viaje",
                column: "RutaId",
                principalTable: "Ruta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
