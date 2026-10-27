
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;
using System.ComponentModel.DataAnnotations;

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

        // Mostrar exámenes pendientes y realizados
        public async Task<IActionResult> Index()
        {
            var examenes = await _context.ExamenesLaboratorio
                .AsNoTracking()
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            ViewBag.Visitas = await _context.VisitasMedicas
                .AsNoTracking().ToListAsync();

            ViewBag.Citas = await _context.CitasMedicas
                .AsNoTracking().ToListAsync();

            ViewBag.Pacientes = await _context.Pacientes
                .AsNoTracking().ToListAsync();

            return View(examenes);
        }

        // Formulario para solicitar un examen
        [HttpGet]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create()
        {
            await CargarVisitas();

            return View(new ExamenLaboratorio
            {
                Fecha = DateTime.Now,
                Costo = 0,
                Resultado = ""
            });
        }

        // Guardar solicitud de examen
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create(
            ExamenLaboratorio examen)
        {
            examen.Id = 0;
            examen.Resultado = "";

            ModelState.Remove(nameof(ExamenLaboratorio.Id));
            ModelState.Remove(nameof(ExamenLaboratorio.Resultado));

            if (!await _context.VisitasMedicas
                .AnyAsync(v => v.Id == examen.VisitaMedicaId))
            {
                ModelState.AddModelError(
                    nameof(examen.VisitaMedicaId),
                    "Seleccione una visita médica válida.");
            }

            if (string.IsNullOrWhiteSpace(examen.NombreExamen))
            {
                ModelState.AddModelError(
                    nameof(examen.NombreExamen),
                    "Ingrese el nombre del examen.");
            }
            else if (examen.NombreExamen.Trim().Length > 200)
            {
                ModelState.AddModelError(
                    nameof(examen.NombreExamen),
                    "El nombre no puede superar 200 caracteres.");
            }

            if (examen.Fecha == default ||
                examen.Fecha > DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(examen.Fecha),
                    "Ingrese una fecha válida que no sea futura.");
            }

            if (examen.Costo < 0)
            {
                ModelState.AddModelError(
                    nameof(examen.Costo),
                    "El costo no puede ser negativo.");
            }

            if (!ModelState.IsValid)
            {
                await CargarVisitas();
                return View(examen);
            }

            examen.NombreExamen = examen.NombreExamen.Trim();

            _context.ExamenesLaboratorio.Add(examen);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "El examen fue solicitado correctamente. Está pendiente de resultados.";

            return RedirectToAction(nameof(Index));
        }

        // Mostrar formulario para registrar resultado
        [HttpGet]
        [Authorize(Roles = "Administrador,Laboratorio")]
        public async Task<IActionResult> Resultado(int id)
        {
            var examen = await _context.ExamenesLaboratorio
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (examen == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(examen.Resultado))
            {
                TempData["Aviso"] =
                    "Este examen ya tiene un resultado registrado.";

                return RedirectToAction(nameof(Index));
            }

            return View(examen);
        }

        // Guardar resultado sin modificar los otros datos
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Laboratorio")]
        public async Task<IActionResult> Resultado(
            int id,
            string? resultado)
        {
            if (string.IsNullOrWhiteSpace(resultado))
            {
                ModelState.AddModelError(
                    "resultado",
                    "Ingrese el resultado del examen.");
            }
            else if (resultado.Length > 4000)
            {
                ModelState.AddModelError(
                    "resultado",
                    "El resultado no puede superar 4000 caracteres.");
            }

            var examen = await _context.ExamenesLaboratorio
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (examen == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(examen.Resultado))
            {
                TempData["Aviso"] =
                    "Este examen ya tiene un resultado registrado.";

                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ResultadoIngresado = resultado;
                return View(examen);
            }

            // Actualización condicional:
            // evita sobrescribir un resultado ya guardado
            int actualizados = await _context.ExamenesLaboratorio
                .Where(e => e.Id == id &&
                    (e.Resultado == null || e.Resultado == ""))
                .ExecuteUpdateAsync(cambios => cambios
                    .SetProperty(
                        e => e.Resultado,
                        resultado!.Trim()));

            if (actualizados == 0)
            {
                TempData["Aviso"] =
                    "El resultado ya fue registrado por otro usuario.";
            }
            else
            {
                TempData["Mensaje"] =
                    "Resultado de laboratorio guardado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Cargar visitas médicas
        private async Task CargarVisitas()
        {
            var visitas = await _context.VisitasMedicas
                .AsNoTracking().ToListAsync();

            var citas = await _context.CitasMedicas
                .AsNoTracking().ToListAsync();

            var pacientes = await _context.Pacientes
                .AsNoTracking().ToListAsync();

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
                    Descripcion =
                        (paciente == null
                            ? "Paciente no disponible"
                            : paciente.Nombre + " " + paciente.Apellido)
                        + " - " +
                        v.Fecha.ToString("dd/MM/yyyy")
                };
            }).ToList();

            ViewBag.ListaVisitas = new SelectList(
                opciones, "Id", "Descripcion");
        }
    }
}
