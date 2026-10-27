
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

        // Reportes generales
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

            ViewBag.TotalGastosFundacion =
                await _context.GastosFundacion.SumAsync(g => (decimal?)g.Monto)
                ?? 0m;

            ViewBag.TotalIngresos =
                (decimal)ViewBag.TotalPagos +
                (decimal)ViewBag.TotalDonaciones;

            ViewBag.BalanceFundacion =
                (decimal)ViewBag.TotalIngresos -
                (decimal)ViewBag.TotalGastosFundacion;

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
                medicamentos
                    .Where(m => m.Estado == "Despachado")
                    .Sum(m => m.Costo);

            decimal totalGeneral =
                totalConsultas +
                totalLaboratorio +
                totalMedicamentos;

            ViewBag.Paciente = paciente;
            ViewBag.Citas = citas;
            ViewBag.Examenes = examenes;

            ViewBag.Medicamentos = medicamentos
                .Where(m => m.Estado == "Despachado")
                .ToList();

            ViewBag.TotalConsultas = totalConsultas;
            ViewBag.TotalLaboratorio = totalLaboratorio;
            ViewBag.TotalMedicamentos = totalMedicamentos;
            ViewBag.TotalGeneral = totalGeneral;

            return View();
        }

        // Cobros por fechas
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

            ViewBag.NombresPacientes = pacientes;
            ViewBag.TotalCobrado = pagos.Sum(p => p.Monto);
            ViewBag.CantidadPagos = pagos.Count;

            return View(pagos);
        }

        // Reporte de ingresos de la fundación
        [HttpGet]
        public async Task<IActionResult> Ingresos()
        {
            var pagos = await _context.Pagos
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            var donaciones = await _context.Donaciones
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();

            ViewBag.Pagos = pagos;
            ViewBag.Donaciones = donaciones;

            ViewBag.TotalPagos = pagos.Sum(p => p.Monto);
            ViewBag.TotalDonaciones = donaciones.Sum(d => d.Monto);

            ViewBag.TotalIngresos =
                (decimal)ViewBag.TotalPagos +
                (decimal)ViewBag.TotalDonaciones;

            return View();
        }

        // Reporte de gastos de la fundación
        [HttpGet]
        public async Task<IActionResult> GastosFundacion()
        {
            var gastos = await _context.GastosFundacion
                .OrderByDescending(g => g.Fecha)
                .ToListAsync();

            ViewBag.TotalGastos = gastos.Sum(g => g.Monto);

            return View(gastos);
        }

        // Reporte de laboratorios por paciente
        [HttpGet]
        public async Task<IActionResult> LaboratoriosPaciente(int pacienteId)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == pacienteId);

            if (paciente == null)
            {
                return NotFound();
            }

            var citas = await _context.CitasMedicas
                .Where(c => c.PacienteId == pacienteId)
                .Select(c => c.Id)
                .ToListAsync();

            var visitas = await _context.VisitasMedicas
                .Where(v => citas.Contains(v.CitaMedicaId))
                .Select(v => v.Id)
                .ToListAsync();

            var examenes = await _context.ExamenesLaboratorio
                .Where(e => visitas.Contains(e.VisitaMedicaId))
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            ViewBag.Paciente = paciente;
            ViewBag.TotalLaboratorio = examenes.Sum(e => e.Costo);

            return View(examenes);
        }

        // Reporte de medicamentos por paciente
        [HttpGet]
        public async Task<IActionResult> MedicamentosPaciente(int pacienteId)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == pacienteId);

            if (paciente == null)
            {
                return NotFound();
            }

            var citas = await _context.CitasMedicas
                .Where(c => c.PacienteId == pacienteId)
                .Select(c => c.Id)
                .ToListAsync();

            var visitas = await _context.VisitasMedicas
                .Where(v => citas.Contains(v.CitaMedicaId))
                .Select(v => v.Id)
                .ToListAsync();

            var medicamentos = await _context.Medicamentos
                .Where(m => m.VisitaMedicaId.HasValue &&
                    visitas.Contains(m.VisitaMedicaId.Value))
                .ToListAsync();

            ViewBag.Paciente = paciente;

            ViewBag.TotalMedicamentos = medicamentos
                .Where(m => m.Estado == "Despachado")
                .Sum(m => m.Costo);

            return View(medicamentos);
        }

        // Reporte de consultas con saldos pendientes
        [HttpGet]
        public async Task<IActionResult> SaldosPendientes()
        {
            var citas = await _context.CitasMedicas
                .Where(c =>
                    c.CostoConsulta - c.Descuento > c.MontoPagado)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var pacientes = await _context.Pacientes
                .ToDictionaryAsync(
                    p => p.Id,
                    p => p.Nombre + " " + p.Apellido);

            ViewBag.NombresPacientes = pacientes;

            ViewBag.TotalPendiente = citas.Sum(c =>
                c.CostoConsulta - c.Descuento - c.MontoPagado);

            return View(citas);
        }

        // Reporte de historial y análisis médicos
        [HttpGet]
        public async Task<IActionResult> HistorialPaciente(int pacienteId)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == pacienteId);

            if (paciente == null)
            {
                return NotFound();
            }

            var historial = await _context.HistorialesMedicos
                .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

            var idsCitas = await _context.CitasMedicas
                .Where(c => c.PacienteId == pacienteId)
                .Select(c => c.Id)
                .ToListAsync();

            var idsVisitas = await _context.VisitasMedicas
                .Where(v => idsCitas.Contains(v.CitaMedicaId))
                .Select(v => v.Id)
                .ToListAsync();

            var medicamentos = await _context.Medicamentos
                .Where(m => m.VisitaMedicaId.HasValue &&
                    idsVisitas.Contains(m.VisitaMedicaId.Value))
                .ToListAsync();

            ViewBag.Paciente = paciente;
            ViewBag.Medicamentos = medicamentos;

            return View(historial);
        }
        
        // Reporte de gastos por cada visita médica
        [HttpGet]
        public async Task<IActionResult> GastosPorVisita(int pacienteId)
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
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            var idsVisitas = visitas.Select(v => v.Id).ToList();

            var examenes = await _context.ExamenesLaboratorio
                .Where(e => idsVisitas.Contains(e.VisitaMedicaId))
                .ToListAsync();

            var medicamentos = await _context.Medicamentos
                .Where(m => m.VisitaMedicaId.HasValue &&
                    idsVisitas.Contains(m.VisitaMedicaId.Value))
                .ToListAsync();

            ViewBag.Paciente = paciente;
            ViewBag.Citas = citas;
            ViewBag.Examenes = examenes;
            ViewBag.Medicamentos = medicamentos;

            return View(visitas);
        }
    }
}
