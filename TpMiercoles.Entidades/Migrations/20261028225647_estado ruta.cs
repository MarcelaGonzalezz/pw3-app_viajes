using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TpMiercoles.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class estadoruta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parada_Ruta_RutaId",
                table: "Parada");

            migrationBuilder.DropForeignKey(
                name: "FK_Ruta_Direccion_DireccionDestinoId",
                table: "Ruta");

            migrationBuilder.DropForeignKey(
                name: "FK_Ruta_Direccion_DireccionOrigenId",
                table: "Ruta");

            migrationBuilder.DropForeignKey(
                name: "FK_Ruta_Usuarios_UsuarioId",
                table: "Ruta");

            migrationBuilder.DropForeignKey(
                name: "FK_RutaDia_Ruta_RutaId",
                table: "RutaDia");

            migrationBuilder.DropForeignKey(
                name: "FK_Viaje_Ruta_RutaId",
                table: "Viaje");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ruta",
                table: "Ruta");

            migrationBuilder.RenameTable(
                name: "Ruta",
                newName: "Rutas");

            migrationBuilder.RenameIndex(
                name: "IX_Ruta_UsuarioId",
                table: "Rutas",
                newName: "IX_Rutas_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Ruta_DireccionOrigenId",
                table: "Rutas",
                newName: "IX_Rutas_DireccionOrigenId");

            migrationBuilder.RenameIndex(
                name: "IX_Ruta_DireccionDestinoId",
                table: "Rutas",
                newName: "IX_Rutas_DireccionDestinoId");

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Rutas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rutas",
                table: "Rutas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parada_Rutas_RutaId",
                table: "Parada",
                column: "RutaId",
                principalTable: "Rutas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RutaDia_Rutas_RutaId",
                table: "RutaDia",
                column: "RutaId",
                principalTable: "Rutas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rutas_Direccion_DireccionDestinoId",
                table: "Rutas",
                column: "DireccionDestinoId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rutas_Direccion_DireccionOrigenId",
                table: "Rutas",
                column: "DireccionOrigenId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rutas_Usuarios_UsuarioId",
                table: "Rutas",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Viaje_Rutas_RutaId",
                table: "Viaje",
                column: "RutaId",
                principalTable: "Rutas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parada_Rutas_RutaId",
                table: "Parada");

            migrationBuilder.DropForeignKey(
                name: "FK_RutaDia_Rutas_RutaId",
                table: "RutaDia");

            migrationBuilder.DropForeignKey(
                name: "FK_Rutas_Direccion_DireccionDestinoId",
                table: "Rutas");

            migrationBuilder.DropForeignKey(
                name: "FK_Rutas_Direccion_DireccionOrigenId",
                table: "Rutas");

            migrationBuilder.DropForeignKey(
                name: "FK_Rutas_Usuarios_UsuarioId",
                table: "Rutas");

            migrationBuilder.DropForeignKey(
                name: "FK_Viaje_Rutas_RutaId",
                table: "Viaje");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rutas",
                table: "Rutas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Rutas");

            migrationBuilder.RenameTable(
                name: "Rutas",
                newName: "Ruta");

            migrationBuilder.RenameIndex(
                name: "IX_Rutas_UsuarioId",
                table: "Ruta",
                newName: "IX_Ruta_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Rutas_DireccionOrigenId",
                table: "Ruta",
                newName: "IX_Ruta_DireccionOrigenId");

            migrationBuilder.RenameIndex(
                name: "IX_Rutas_DireccionDestinoId",
                table: "Ruta",
                newName: "IX_Ruta_DireccionDestinoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ruta",
                table: "Ruta",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parada_Ruta_RutaId",
                table: "Parada",
                column: "RutaId",
                principalTable: "Ruta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ruta_Direccion_DireccionDestinoId",
                table: "Ruta",
                column: "DireccionDestinoId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ruta_Direccion_DireccionOrigenId",
                table: "Ruta",
                column: "DireccionOrigenId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ruta_Usuarios_UsuarioId",
                table: "Ruta",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RutaDia_Ruta_RutaId",
                table: "RutaDia",
                column: "RutaId",
                principalTable: "Ruta",
                principalColumn: "Id");

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
