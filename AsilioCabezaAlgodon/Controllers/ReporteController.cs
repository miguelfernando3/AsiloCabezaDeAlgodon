
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

            // Consultas con descuento
            decimal totalConsultas = citas.Sum(c =>
                Math.Max(0m, c.CostoConsulta - c.Descuento));

            // Laboratorios con descuento
            decimal totalLaboratorio = examenes.Sum(e =>
                Math.Max(0m, e.Costo - e.Descuento));

            // Solo medicamentos despachados, con descuento
            decimal totalMedicamentos = medicamentos
                .Where(m => m.Estado == "Despachado")
                .Sum(m => Math.Max(0m, m.Costo - m.Descuento));

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

        // Cobros por paciente y fechas
        [HttpGet]
        public async Task<IActionResult> Cobros(
            DateTime? fechaInicio, DateTime? fechaFin, int? pacienteId)
        {
            DateTime inicio = fechaInicio?.Date
                ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            DateTime fin = fechaFin?.Date ?? DateTime.Today;

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;
            ViewBag.PacienteId = pacienteId;

            var pacientes = await _context.Pacientes
                .OrderBy(p => p.Nombre)
                .ThenBy(p => p.Apellido)
                .ToListAsync();

            ViewBag.ListaPacientes = new SelectList(
                pacientes.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " " + p.Apellido
                }),
                "Id",
                "NombreCompleto",
                pacienteId);

            ViewBag.NombresPacientes = pacientes.ToDictionary(
                p => p.Id,
                p => p.Nombre + " " + p.Apellido);

            ViewBag.TotalCobrado = 0m;
            ViewBag.CantidadPagos = 0;
            ViewBag.CitasDetalle = new List<CitaMedica>();
            ViewBag.ExamenesDetalle = new List<ExamenLaboratorio>();
            ViewBag.MedicamentosDetalle = new List<Medicamento>();
            ViewBag.TotalConsultas = 0m;
            ViewBag.TotalLaboratorio = 0m;
            ViewBag.TotalMedicamentos = 0m;
            ViewBag.TotalGastosMedicos = 0m;

            if (inicio > fin)
            {
                ModelState.AddModelError(
                    "",
                    "La fecha inicial no puede ser mayor que la fecha final.");

                return View(new List<Pago>());
            }

            if (pacienteId.HasValue &&
                !pacientes.Any(p => p.Id == pacienteId.Value))
            {
                ModelState.AddModelError(
                    "",
                    "El paciente seleccionado no existe.");

                return View(new List<Pago>());
            }

            DateTime finExclusivo = fin.AddDays(1);

            var pagosQuery = _context.Pagos.Where(p =>
                p.Fecha >= inicio &&
                p.Fecha < finExclusivo);

            var citasQuery = _context.CitasMedicas.Where(c =>
                c.Fecha >= inicio &&
                c.Fecha < finExclusivo);

            if (pacienteId.HasValue)
            {
                pagosQuery = pagosQuery.Where(p =>
                    p.PacienteId == pacienteId.Value);

                citasQuery = citasQuery.Where(c =>
                    c.PacienteId == pacienteId.Value);
            }

            var pagos = await pagosQuery
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            var citas = await citasQuery
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            // Laboratorios por fecha de examen.
            // Medicamentos por fecha de visita.
            var citasPacienteQuery =
                _context.CitasMedicas.AsQueryable();

            if (pacienteId.HasValue)
            {
                citasPacienteQuery = citasPacienteQuery.Where(c =>
                    c.PacienteId == pacienteId.Value);
            }

            var idsCitasPaciente = await citasPacienteQuery
                .Select(c => c.Id)
                .ToListAsync();

            var visitasPaciente = await _context.VisitasMedicas
                .Where(v => idsCitasPaciente.Contains(v.CitaMedicaId))
                .ToListAsync();

            var idsVisitasPaciente = visitasPaciente
                .Select(v => v.Id)
                .ToList();

            var idsVisitasEnPeriodo = visitasPaciente
                .Where(v =>
                    v.Fecha >= inicio &&
                    v.Fecha < finExclusivo)
                .Select(v => v.Id)
                .ToList();

            var examenes = await _context.ExamenesLaboratorio
                .Where(e =>
                    idsVisitasPaciente.Contains(e.VisitaMedicaId) &&
                    e.Fecha >= inicio &&
                    e.Fecha < finExclusivo)
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            var medicamentos = await _context.Medicamentos
                .Where(m =>
                    m.VisitaMedicaId.HasValue &&
                    idsVisitasEnPeriodo.Contains(m.VisitaMedicaId.Value) &&
                    m.Estado == "Despachado")
                .ToListAsync();

            decimal totalConsultas = citas.Sum(c =>
                Math.Max(0m, c.CostoConsulta - c.Descuento));

            decimal totalLaboratorio = examenes.Sum(e =>
                Math.Max(0m, e.Costo - e.Descuento));

            decimal totalMedicamentos = medicamentos.Sum(m =>
                Math.Max(0m, m.Costo - m.Descuento));

            ViewBag.TotalCobrado = pagos.Sum(p => p.Monto);
            ViewBag.CantidadPagos = pagos.Count;
            ViewBag.CitasDetalle = citas;
            ViewBag.ExamenesDetalle = examenes;
            ViewBag.MedicamentosDetalle = medicamentos;

            ViewBag.TotalConsultas = totalConsultas;
            ViewBag.TotalLaboratorio = totalLaboratorio;
            ViewBag.TotalMedicamentos = totalMedicamentos;

            ViewBag.TotalGastosMedicos =
                totalConsultas +
                totalLaboratorio +
                totalMedicamentos;

            return View(pagos);
        }

        // Ingresos
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

        // Gastos de la fundación
        [HttpGet]
        public async Task<IActionResult> GastosFundacion()
        {
            var gastos = await _context.GastosFundacion
                .OrderByDescending(g => g.Fecha)
                .ToListAsync();

            ViewBag.TotalGastos = gastos.Sum(g => g.Monto);

            return View(gastos);
        }

        // Laboratorios por paciente
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

            ViewBag.TotalLaboratorio = examenes.Sum(e =>
                Math.Max(0m, e.Costo - e.Descuento));

            return View(examenes);
        }

        // Medicamentos por paciente
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
                .Where(m =>
                    m.VisitaMedicaId.HasValue &&
                    visitas.Contains(m.VisitaMedicaId.Value))
                .ToListAsync();

            ViewBag.Paciente = paciente;

            ViewBag.TotalMedicamentos = medicamentos
                .Where(m => m.Estado == "Despachado")
                .Sum(m => Math.Max(0m, m.Costo - m.Descuento));

            return View(medicamentos);
        }

        // Saldos pendientes de consultas
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
                Math.Max(
                    0m,
                    c.CostoConsulta -
                    c.Descuento -
                    c.MontoPagado));

            return View(citas);
        }

        // Historial médico
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
                .Where(m =>
                    m.VisitaMedicaId.HasValue &&
                    idsVisitas.Contains(m.VisitaMedicaId.Value))
                .ToListAsync();

            ViewBag.Paciente = paciente;
            ViewBag.Medicamentos = medicamentos;

            return View(historial);
        }

        // Gastos por visita
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
                .Where(m =>
                    m.VisitaMedicaId.HasValue &&
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
