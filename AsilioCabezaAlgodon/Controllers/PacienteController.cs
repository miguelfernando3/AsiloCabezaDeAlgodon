
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Recepcionista,Enfermero")]
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

            // NUEVO: Buscar los exámenes de laboratorio
            // relacionados con las visitas del paciente
            var examenes = await _context.ExamenesLaboratorio
                .Where(e => idsVisitas.Contains(e.VisitaMedicaId))
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            // Buscar los médicos de las citas
            var idsMedicos = citas.Select(c => c.MedicoId)
                .Distinct()
                .ToList();

            var medicos = await _context.Medicos
                .Where(m => idsMedicos.Contains(m.Id))
                .ToListAsync();

            // Enviar la información a la vista
            ViewBag.Citas = citas;
            ViewBag.Visitas = visitas;
            ViewBag.Diagnosticos = diagnosticos;
            ViewBag.Medicos = medicos;

            // NUEVO: Enviar exámenes al historial clínico
            ViewBag.Examenes = examenes;
            // Buscar medicamentos asociados a las visitas del paciente
            var medicamentos = await _context.Medicamentos
            .Where(m => m.VisitaMedicaId.HasValue &&
                idsVisitas.Contains(m.VisitaMedicaId.Value))
                .ToListAsync();
                
                // Enviar medicamentos al historial clínico
            ViewBag.Medicamentos = medicamentos;

            return View(paciente);
        }
    }
}
