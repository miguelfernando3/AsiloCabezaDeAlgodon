using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsilioCabezaAlgodon.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMensualidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Anio",
                table: "Pagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsMensualidad",
                table: "Pagos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Mes",
                table: "Pagos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anio",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "EsMensualidad",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "Mes",
                table: "Pagos");
        }
    }
}
