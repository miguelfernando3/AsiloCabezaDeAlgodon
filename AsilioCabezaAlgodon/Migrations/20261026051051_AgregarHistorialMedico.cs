using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsilioCabezaAlgodon.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHistorialMedico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialesMedicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    MotivoIngreso = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AntecedentesMedicos = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EnfermedadesCronicas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Alergias = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Psicopatologias = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MedicamentosHabituales = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesMedicos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialesMedicos");
        }
    }
}
