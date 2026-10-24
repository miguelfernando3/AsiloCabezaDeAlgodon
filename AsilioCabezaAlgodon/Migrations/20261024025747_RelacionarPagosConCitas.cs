using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsilioCabezaAlgodon.Migrations
{
    /// <inheritdoc />
    public partial class RelacionarPagosConCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CitaMedicaId",
                table: "Pagos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CitaMedicaId",
                table: "Pagos");
        }
    }
}
