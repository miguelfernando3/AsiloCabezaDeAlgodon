
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;
using System.ComponentModel.DataAnnotations;

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
            if (familiar.PacienteId.HasValue)
            {
                bool existe = await _context.Pacientes
                    .AnyAsync(p => p.Id == familiar.PacienteId.Value);

                if (!existe)
                {
                    ModelState.AddModelError(
                        nameof(Familiar.PacienteId),
                        "El paciente seleccionado no existe.");
                }
            }

            if (familiar.RecibeNotificaciones &&
                familiar.PacienteId.HasValue)
            {
                bool yaExisteAutorizado = await _context.Familiares
                    .AnyAsync(f =>
                        f.PacienteId == familiar.PacienteId.Value &&
                        f.RecibeNotificaciones);

                if (yaExisteAutorizado)
                {
                    ModelState.AddModelError(
                        nameof(Familiar.RecibeNotificaciones),
                        "Este paciente ya tiene un familiar autorizado para recibir notificaciones.");
                }
            }

            if (familiar.RecibeNotificaciones &&
                string.IsNullOrWhiteSpace(familiar.Correo))
            {
                ModelState.AddModelError(
                    nameof(Familiar.Correo),
                    "Ingrese un correo electrónico para recibir notificaciones.");
            }

            if (!ModelState.IsValid)
            {
                await CargarPacientes();
                return View(familiar);
            }

            _context.Familiares.Add(familiar);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Familiar registrado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Mostrar formulario para editar familiar
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var familiar = await _context.Familiares
                .FirstOrDefaultAsync(f => f.Id == id);

            if (familiar == null)
            {
                return NotFound();
            }

            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == familiar.PacienteId);

            ViewBag.NombrePaciente = paciente == null
                ? "Paciente no encontrado"
                : paciente.Nombre + " " + paciente.Apellido;

            return View(familiar);
        }

        // Guardar cambios del familiar
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string nombre,
            string apellido,
            string parentesco,
            string? telefono,
            string? correo)
        {
            var familiar = await _context.Familiares
                .FirstOrDefaultAsync(f => f.Id == id);

            if (familiar == null)
            {
                return NotFound();
            }

            nombre = nombre?.Trim() ?? "";
            apellido = apellido?.Trim() ?? "";
            parentesco = parentesco?.Trim() ?? "";
            telefono = telefono?.Trim();
            correo = correo?.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError(
                    nameof(Familiar.Nombre),
                    "Ingrese el nombre del familiar.");
            }

            if (string.IsNullOrWhiteSpace(apellido))
            {
                ModelState.AddModelError(
                    nameof(Familiar.Apellido),
                    "Ingrese el apellido del familiar.");
            }

            if (string.IsNullOrWhiteSpace(parentesco))
            {
                ModelState.AddModelError(
                    nameof(Familiar.Parentesco),
                    "Ingrese el parentesco.");
            }

            if (!string.IsNullOrWhiteSpace(correo) &&
                !new EmailAddressAttribute().IsValid(correo))
            {
                ModelState.AddModelError(
                    nameof(Familiar.Correo),
                    "Ingrese un correo electrónico válido.");
            }

            if (familiar.RecibeNotificaciones &&
                string.IsNullOrWhiteSpace(correo))
            {
                ModelState.AddModelError(
                    nameof(Familiar.Correo),
                    "El familiar autorizado debe tener correo electrónico.");
            }

            if (!ModelState.IsValid)
            {
                // Conservar los valores ingresados
                familiar.Nombre = nombre;
                familiar.Apellido = apellido;
                familiar.Parentesco = parentesco;
                familiar.Telefono = telefono ?? "";
                familiar.Correo = correo ?? "";

                var paciente = await _context.Pacientes
                    .FirstOrDefaultAsync(p =>
                        p.Id == familiar.PacienteId);

                ViewBag.NombrePaciente = paciente == null
                    ? "Paciente no encontrado"
                    : paciente.Nombre + " " + paciente.Apellido;

                return View(familiar);
            }

            // Actualizar únicamente los datos permitidos
            familiar.Nombre = nombre;
            familiar.Apellido = apellido;
            familiar.Parentesco = parentesco;
            familiar.Telefono = telefono ?? "";
            familiar.Correo = correo ?? "";

            // No modificar PacienteId ni la autorización
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Los datos del familiar fueron actualizados correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Cambiar autorización de notificaciones
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> CambiarAutorizacion(int id)
        {
            var familiar = await _context.Familiares
                .FirstOrDefaultAsync(f => f.Id == id);

            if (familiar == null)
            {
                return NotFound();
            }

            if (familiar.RecibeNotificaciones)
            {
                familiar.RecibeNotificaciones = false;

                await _context.SaveChangesAsync();

                TempData["Mensaje"] =
                    "Se quitó la autorización correctamente.";

                return RedirectToAction(nameof(Index));
            }

            if (!familiar.PacienteId.HasValue)
            {
                TempData["Error"] =
                    "El familiar no tiene un paciente asignado.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(familiar.Correo))
            {
                TempData["Error"] =
                    "El familiar debe tener un correo electrónico.";

                return RedirectToAction(nameof(Index));
            }

            var anteriores = await _context.Familiares
                .Where(f =>
                    f.PacienteId == familiar.PacienteId &&
                    f.RecibeNotificaciones &&
                    f.Id != familiar.Id)
                .ToListAsync();

            foreach (var anterior in anteriores)
            {
                anterior.RecibeNotificaciones = false;
            }

            familiar.RecibeNotificaciones = true;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "El familiar fue autorizado correctamente.";

            return RedirectToAction(nameof(Index));
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
                    NombreCompleto =
                        p.Nombre + " " + p.Apellido
                })
                .ToListAsync();

            ViewBag.Pacientes = new SelectList(
                pacientes,
                "Id",
                "NombreCompleto");
        }
    }
}
