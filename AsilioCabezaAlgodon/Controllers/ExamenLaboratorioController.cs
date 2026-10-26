
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Medico,Laboratorio")]
    public class ExamenLaboratorioController : Controller
    {
        private readonly AsiloContext _context;

        public ExamenLaboratorioController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar exámenes registrados
        public async Task<IActionResult> Index()
        {
            var examenes = await _context.ExamenesLaboratorio
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            ViewBag.Visitas = await _context.VisitasMedicas
                .ToListAsync();

            ViewBag.Citas = await _context.CitasMedicas
                .ToListAsync();

            ViewBag.Pacientes = await _context.Pacientes
                .ToListAsync();

            return View(examenes);
        }

        // Mostrar formulario de registro
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarVisitas();

            return View(new ExamenLaboratorio
            {
                Fecha = DateTime.Now,
                Costo = 0
            });
        }

        // Guardar examen de laboratorio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ExamenLaboratorio examen)
        {
            if (!await _context.VisitasMedicas
                .AnyAsync(v => v.Id == examen.VisitaMedicaId))
            {
                ModelState.AddModelError(
                    "VisitaMedicaId",
                    "Selecciona una visita médica válida.");
            }

            if (string.IsNullOrWhiteSpace(examen.NombreExamen))
            {
                ModelState.AddModelError(
                    "NombreExamen",
                    "Ingresa el nombre del examen.");
            }

            if (string.IsNullOrWhiteSpace(examen.Resultado))
            {
                ModelState.AddModelError(
                    "Resultado",
                    "Ingresa el resultado del examen.");
            }

            if (examen.Fecha == default)
            {
                ModelState.AddModelError(
                    "Fecha",
                    "Selecciona una fecha válida.");
            }

            // Validar el costo del examen
            if (examen.Costo < 0)
            {
                ModelState.AddModelError(
                    "Costo",
                    "El costo del examen no puede ser negativo.");
            }

            if (!ModelState.IsValid)
            {
                await CargarVisitas();
                return View(examen);
            }

            _context.ExamenesLaboratorio.Add(examen);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Examen de laboratorio registrado correctamente.";

            return RedirectToAction("Index");
        }

        // Cargar visitas médicas disponibles
        private async Task CargarVisitas()
        {
            var visitas = await _context.VisitasMedicas
                .ToListAsync();

            var citas = await _context.CitasMedicas
                .ToListAsync();

            var pacientes = await _context.Pacientes
                .ToListAsync();

            var opciones = visitas.Select(v =>
            {
                var cita = citas.FirstOrDefault(
                    c => c.Id == v.CitaMedicaId);

                var paciente = cita == null
                    ? null
                    : pacientes.FirstOrDefault(
                        p => p.Id == cita.PacienteId);

                return new
                {
                    v.Id,
                    Descripcion = (paciente == null
                        ? "Paciente no disponible"
                        : paciente.Nombre + " " + paciente.Apellido)
                        + " - " + v.Fecha.ToString("dd/MM/yyyy")
                };
            }).ToList();

            ViewBag.ListaVisitas = new SelectList(
                opciones, "Id", "Descripcion");
        }
    }
}
