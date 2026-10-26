
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Recepcionista,Medico,Enfermero")]
    public class PacienteController : Controller
    {
        private readonly AsiloContext _context;

        public PacienteController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar pacientes
        public IActionResult Index()
        {
            var pacientes = _context.Pacientes.ToList();

            return View(pacientes);
        }

        // Mostrar formulario para registrar pacientes
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Guardar un paciente nuevo
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Paciente paciente)
        {
            if (!ModelState.IsValid)
            {
                return View(paciente);
            }

            _context.Pacientes.Add(paciente);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // Mostrar historial clínico de un paciente
        [HttpGet]
        public async Task<IActionResult> Historial(int id)
        {
            // Buscar al paciente
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paciente == null)
            {
                return NotFound();
            }

            // Buscar las citas del paciente
            var citas = await _context.CitasMedicas
                .Where(c => c.PacienteId == id)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            // Obtener los identificadores de sus citas
            var idsCitas = citas.Select(c => c.Id).ToList();

            // Buscar las visitas correspondientes
            var visitas = await _context.VisitasMedicas
                .Where(v => idsCitas.Contains(v.CitaMedicaId))
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            // Obtener los identificadores de las visitas
            var idsVisitas = visitas.Select(v => v.Id).ToList();

            // Buscar los diagnósticos de esas visitas
            var diagnosticos = await _context.Diagnosticos
                .Where(d => idsVisitas.Contains(d.VisitaMedicaId))
                .ToListAsync();

            // Buscar los exámenes de laboratorio
            var examenes = await _context.ExamenesLaboratorio
                .Where(e => idsVisitas.Contains(e.VisitaMedicaId))
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            // Buscar los médicos de las citas
            var idsMedicos = citas
                .Select(c => c.MedicoId)
                .Distinct()
                .ToList();

            var medicos = await _context.Medicos
                .Where(m => idsMedicos.Contains(m.Id))
                .ToListAsync();

            // Buscar medicamentos asociados a las visitas
            var medicamentos = await _context.Medicamentos
                .Where(m => m.VisitaMedicaId.HasValue &&
                    idsVisitas.Contains(m.VisitaMedicaId.Value))
                .ToListAsync();

            // Buscar el historial médico general
            var historialMedico = await _context.HistorialesMedicos
                .FirstOrDefaultAsync(h => h.PacienteId == id);

            // NUEVO: Buscar familiares del paciente
            var familiares = await _context.Familiares
                .Where(f => f.PacienteId == id)
                .OrderBy(f => f.Apellido)
                .ThenBy(f => f.Nombre)
                .ToListAsync();

            // Enviar información a la vista
            ViewBag.Citas = citas;
            ViewBag.Visitas = visitas;
            ViewBag.Diagnosticos = diagnosticos;
            ViewBag.Medicos = medicos;
            ViewBag.Examenes = examenes;
            ViewBag.Medicamentos = medicamentos;
            ViewBag.HistorialMedico = historialMedico;

            // NUEVO: Enviar familiares a la vista
            ViewBag.Familiares = familiares;

            return View(paciente);
        }
    }
}
