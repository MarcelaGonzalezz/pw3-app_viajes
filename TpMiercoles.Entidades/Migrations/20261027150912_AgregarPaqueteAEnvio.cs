using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TpMiercoles.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPaqueteAEnvio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignacion_Envio_EnvioId",
                table: "Asignacion");

            migrationBuilder.DropForeignKey(
                name: "FK_Envio_Direccion_DireccionEntregaId",
                table: "Envio");

            migrationBuilder.DropForeignKey(
                name: "FK_Envio_Direccion_DireccionRetiroId",
                table: "Envio");

            migrationBuilder.DropForeignKey(
                name: "FK_Envio_Usuarios_UsuarioId",
                table: "Envio");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Envio",
                table: "Envio");

            migrationBuilder.RenameTable(
                name: "Envio",
                newName: "Envios");

            migrationBuilder.RenameIndex(
                name: "IX_Envio_UsuarioId",
                table: "Envios",
                newName: "IX_Envios_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Envio_DireccionRetiroId",
                table: "Envios",
                newName: "IX_Envios_DireccionRetiroId");

            migrationBuilder.RenameIndex(
                name: "IX_Envio_DireccionEntregaId",
                table: "Envios",
                newName: "IX_Envios_DireccionEntregaId");

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Envios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Monto",
                table: "Envios",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Paquete_Descripcion",
                table: "Envios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Paquete_PesoKg",
                table: "Envios",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Paquete_Tamanio",
                table: "Envios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Envios",
                table: "Envios",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignacion_Envios_EnvioId",
                table: "Asignacion",
                column: "EnvioId",
                principalTable: "Envios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Envios_Direccion_DireccionEntregaId",
                table: "Envios",
                column: "DireccionEntregaId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Envios_Direccion_DireccionRetiroId",
                table: "Envios",
                column: "DireccionRetiroId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Envios_Usuarios_UsuarioId",
                table: "Envios",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignacion_Envios_EnvioId",
                table: "Asignacion");

            migrationBuilder.DropForeignKey(
                name: "FK_Envios_Direccion_DireccionEntregaId",
                table: "Envios");

            migrationBuilder.DropForeignKey(
                name: "FK_Envios_Direccion_DireccionRetiroId",
                table: "Envios");

            migrationBuilder.DropForeignKey(
                name: "FK_Envios_Usuarios_UsuarioId",
                table: "Envios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Envios",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "Monto",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "Paquete_Descripcion",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "Paquete_PesoKg",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "Paquete_Tamanio",
                table: "Envios");

            migrationBuilder.RenameTable(
                name: "Envios",
                newName: "Envio");

            migrationBuilder.RenameIndex(
                name: "IX_Envios_UsuarioId",
                table: "Envio",
                newName: "IX_Envio_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Envios_DireccionRetiroId",
                table: "Envio",
                newName: "IX_Envio_DireccionRetiroId");

            migrationBuilder.RenameIndex(
                name: "IX_Envios_DireccionEntregaId",
                table: "Envio",
                newName: "IX_Envio_DireccionEntregaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Envio",
                table: "Envio",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignacion_Envio_EnvioId",
                table: "Asignacion",
                column: "EnvioId",
                principalTable: "Envio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Envio_Direccion_DireccionEntregaId",
                table: "Envio",
                column: "DireccionEntregaId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Envio_Direccion_DireccionRetiroId",
                table: "Envio",
                column: "DireccionRetiroId",
                principalTable: "Direccion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Envio_Usuarios_UsuarioId",
                table: "Envio",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
