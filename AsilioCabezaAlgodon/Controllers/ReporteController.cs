
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReporteController : Controller
    {
        private readonly AsiloContext _context;

        public ReporteController(AsiloContext context)
        {
            _context = context;
        }

        // Reportes generales del asilo
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalPacientes =
                await _context.Pacientes.CountAsync();

            ViewBag.TotalCitas =
                await _context.CitasMedicas.CountAsync();

            ViewBag.TotalVisitas =
                await _context.VisitasMedicas.CountAsync();

            ViewBag.TotalFamiliares =
                await _context.Familiares.CountAsync();

            ViewBag.TotalPagos =
                await _context.Pagos.SumAsync(p => (decimal?)p.Monto)
                ?? 0m;

            ViewBag.TotalDonaciones =
                await _context.Donaciones.SumAsync(d => (decimal?)d.Monto)
                ?? 0m;

            // Pacientes disponibles para consultar gastos
            var pacientes = await _context.Pacientes
                .OrderBy(p => p.Nombre)
                .ThenBy(p => p.Apellido)
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " " + p.Apellido
                })
                .ToListAsync();

            ViewBag.ListaPacientes = new SelectList(
                pacientes, "Id", "NombreCompleto");

            return View();
        }

        // Gastos médicos de un paciente
        [HttpGet]
        public async Task<IActionResult> GastosPaciente(int pacienteId)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == pacienteId);

            if (paciente == null)
            {
                return NotFound();
            }

            var citas = await _context.CitasMedicas
                .Where(c => c.PacienteId == pacienteId)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            var idsCitas = citas.Select(c => c.Id).ToList();

            var visitas = await _context.VisitasMedicas
                .Where(v => idsCitas.Contains(v.CitaMedicaId))
                .ToListAsync();

            var idsVisitas = visitas.Select(v => v.Id).ToList();

            var examenes = await _context.ExamenesLaboratorio
                .Where(e => idsVisitas.Contains(e.VisitaMedicaId))
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            var medicamentos = await _context.Medicamentos
                .Where(m => m.VisitaMedicaId.HasValue &&
                    idsVisitas.Contains(m.VisitaMedicaId.Value))
                .ToListAsync();

            decimal totalConsultas = citas.Sum(c =>
                Math.Max(0m, c.CostoConsulta - c.Descuento));

            decimal totalLaboratorio =
                examenes.Sum(e => e.Costo);

            decimal totalMedicamentos =
                medicamentos.Sum(m => m.Costo);

            decimal totalGeneral =
                totalConsultas +
                totalLaboratorio +
                totalMedicamentos;

            ViewBag.Paciente = paciente;
            ViewBag.Citas = citas;
            ViewBag.Examenes = examenes;
            ViewBag.Medicamentos = medicamentos;

            ViewBag.TotalConsultas = totalConsultas;
            ViewBag.TotalLaboratorio = totalLaboratorio;
            ViewBag.TotalMedicamentos = totalMedicamentos;
            ViewBag.TotalGeneral = totalGeneral;

            return View();
        }

        // Reporte de cobros por rango de fechas
        [HttpGet]
        public async Task<IActionResult> Cobros(
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            DateTime inicio = fechaInicio?.Date
                ?? new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1);

            DateTime fin = fechaFin?.Date
                ?? DateTime.Today;

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;

            if (inicio > fin)
            {
                ModelState.AddModelError(
                    "",
                    "La fecha inicial no puede ser mayor que la fecha final.");

                ViewBag.NombresPacientes =
                    new Dictionary<int, string>();

                ViewBag.TotalCobrado = 0m;
                ViewBag.CantidadPagos = 0;

                return View(new List<Pago>());
            }

            var pagos = await _context.Pagos
                .Where(p => p.Fecha >= inicio &&
                            p.Fecha < fin.AddDays(1))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            var pacientes = await _context.Pacientes
                .ToDictionaryAsync(
                    p => p.Id,
                    p => p.Nombre + " " + p.Apellido);

            decimal totalCobrado = pagos.Sum(p => p.Monto);

            ViewBag.NombresPacientes = pacientes;
            ViewBag.TotalCobrado = totalCobrado;
            ViewBag.CantidadPagos = pagos.Count;

            return View(pagos);
        }
    }
}
