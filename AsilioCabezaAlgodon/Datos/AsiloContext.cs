using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Datos
{
    public class AsiloContext : DbContext
    {
        public AsiloContext(DbContextOptions<AsiloContext> options)
            : base(options)
        {
        }

        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Familiar> Familiares { get; set; }
        public DbSet<Medico> Medicos { get; set; }
        public DbSet<CitaMedica> CitasMedicas { get; set; }
        public DbSet<VisitaMedica> VisitasMedicas { get; set; }
        public DbSet<Diagnostico> Diagnosticos { get; set; }
        public DbSet<ExamenLaboratorio> ExamenesLaboratorio { get; set; }
        public DbSet<Medicamento> Medicamentos { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Donacion> Donaciones { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
    }
}