
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsilioCabezaAlgodon.Migrations
{
    public partial class RelacionFamiliarPaciente : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PacienteId",
                table: "Familiares",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PacienteId",
                table: "Familiares");
        }
    }
}
