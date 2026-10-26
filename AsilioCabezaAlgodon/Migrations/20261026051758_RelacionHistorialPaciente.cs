using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsilioCabezaAlgodon.Migrations
{
    /// <inheritdoc />
    public partial class RelacionHistorialPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HistorialesMedicos_PacienteId",
                table: "HistorialesMedicos",
                column: "PacienteId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialesMedicos_Pacientes_PacienteId",
                table: "HistorialesMedicos",
                column: "PacienteId",
                principalTable: "Pacientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistorialesMedicos_Pacientes_PacienteId",
                table: "HistorialesMedicos");

            migrationBuilder.DropIndex(
                name: "IX_HistorialesMedicos_PacienteId",
                table: "HistorialesMedicos");
        }
    }
}
