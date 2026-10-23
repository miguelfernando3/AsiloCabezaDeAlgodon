
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Recepcionista,Fundacion")]
    public class PagoController : Controller
    {
        private readonly AsiloContext _context;

        public PagoController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar pagos registrados
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pagos = await _context.Pagos
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            // Obtener nombres completos de los pacientes
            var pacientes = await _context.Pacientes
                .ToDictionaryAsync(
                    p => p.Id,
                    p => p.Nombre + " " + p.Apellido
                );

            ViewBag.NombresPacientes = pacientes;

            return View(pagos);
        }

        // Mostrar formulario para registrar pagos
        [HttpGet]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> Create()
        {
            await CargarPacientes();
            return View(new Pago());
        }

        // Guardar pago
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> Create(Pago pago)
        {
            bool existePaciente = await _context.Pacientes
                .AnyAsync(p => p.Id == pago.PacienteId);

            if (!existePaciente)
            {
                ModelState.AddModelError(
                    "PacienteId",
                    "Seleccione un paciente válido.");
            }

            if (!ModelState.IsValid)
            {
                await CargarPacientes();
                return View(pago);
            }

            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Cargar pacientes en la lista desplegable
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

            ViewBag.ListaPacientes = new SelectList(
                pacientes,
                "Id",
                "NombreCompleto");
        }
    }
}
