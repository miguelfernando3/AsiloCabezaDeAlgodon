
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Recepcionista,Medico,Enfermero")]
    public class FamiliarController : Controller
    {
        private readonly AsiloContext _context;

        public FamiliarController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar todos los familiares registrados
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var familiares = await _context.Familiares
                .OrderBy(f => f.Apellido)
                .ThenBy(f => f.Nombre)
                .ToListAsync();

            var pacientes = await _context.Pacientes.ToListAsync();

            ViewBag.Pacientes = pacientes;

            return View(familiares);
        }

        // Mostrar formulario de registro
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpGet]
        public async Task<IActionResult> Create(int? pacienteId)
        {
            await CargarPacientes();

            var familiar = new Familiar();

            if (pacienteId.HasValue)
            {
                familiar.PacienteId = pacienteId.Value;
            }

            return View(familiar);
        }

        // Guardar familiar
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Familiar familiar)
        {
            // Comprobar que el paciente exista
            if (familiar.PacienteId.HasValue)
            {
                bool existe = await _context.Pacientes
                    .AnyAsync(p => p.Id == familiar.PacienteId.Value);

                if (!existe)
                {
                    ModelState.AddModelError(
                        "PacienteId",
                        "El paciente seleccionado no existe.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarPacientes();
                return View(familiar);
            }

            _context.Familiares.Add(familiar);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Familiar registrado correctamente.";

            return RedirectToAction("Index");
        }

        // Cargar pacientes para el formulario
        
private async Task CargarPacientes()
{
    var pacientes = await _context.Pacientes
        .OrderBy(p => p.Nombre)
        .ThenBy(p => p.Apellido)
        .Select(p => new
        {
            p.Id,
            NombreCompleto = p.Nombre + " " + p.Apellido
        })
        .ToListAsync();

    ViewBag.Pacientes = new SelectList(
        pacientes,
        "Id",
        "NombreCompleto");
        }
    }
}
