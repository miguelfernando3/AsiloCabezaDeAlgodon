
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;
using AsilioCabezaAlgodon.Models;
using System.Data;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Medico,Enfermero")]
    public class VisitaMedicaController : Controller
    {
        private readonly AsiloContext _context;

        public VisitaMedicaController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar visitas médicas
        public async Task<IActionResult> Index()
        {
            var visitas = await _context.VisitasMedicas
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            ViewBag.Citas = await _context.CitasMedicas.ToListAsync();
            ViewBag.Pacientes = await _context.Pacientes.ToListAsync();
            ViewBag.Medicos = await _context.Medicos.ToListAsync();
            ViewBag.Diagnosticos = await _context.Diagnosticos.ToListAsync();

            return View(visitas);
        }

        // Formulario para registrar visita
        [HttpGet]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create()
        {
            await CargarCitas();
            return View(new RegistrarVisitaViewModel());
        }

        // Guardar visita y diagnóstico
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create(
            RegistrarVisitaViewModel modelo)
        {
            // Validar fecha
            if (modelo.Fecha > DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(modelo.Fecha),
                    "La fecha de la visita no puede ser futura.");
            }

            if (modelo.Fecha == default)
            {
                ModelState.AddModelError(
                    nameof(modelo.Fecha),
                    "Ingrese una fecha válida.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCitas();
                return View(modelo);
            }

            // Transacción para evitar duplicaciones simultáneas
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var cita = await _context.CitasMedicas
                .FirstOrDefaultAsync(c =>
                    c.Id == modelo.CitaMedicaId);

            if (cita == null)
            {
                ModelState.AddModelError(
                    nameof(modelo.CitaMedicaId),
                    "La cita médica no existe.");
            }
            else if (cita.Estado != "Programada")
            {
                ModelState.AddModelError(
                    nameof(modelo.CitaMedicaId),
                    "Esta cita ya no está programada.");
            }
            else
            {
                bool existeVisita = await _context.VisitasMedicas
                    .AnyAsync(v =>
                        v.CitaMedicaId == modelo.CitaMedicaId);

                if (existeVisita)
                {
                    ModelState.AddModelError(
                        nameof(modelo.CitaMedicaId),
                        "Esta cita ya tiene una visita registrada.");
                }
            }

            if (!ModelState.IsValid)
            {
                await transaccion.RollbackAsync();
                await CargarCitas();
                return View(modelo);
            }

            var visita = new VisitaMedica
            {
                CitaMedicaId = modelo.CitaMedicaId,
                Fecha = modelo.Fecha,
                Motivo = modelo.Motivo,
                Diagnostico = modelo.Diagnostico,
                Observaciones = modelo.Observaciones
            };

            _context.VisitasMedicas.Add(visita);
            await _context.SaveChangesAsync();

            var diagnostico = new Diagnostico
            {
                VisitaMedicaId = visita.Id,
                Descripcion = modelo.Diagnostico,
                Fecha = modelo.Fecha
            };

            _context.Diagnosticos.Add(diagnostico);

            // Marcar cita como atendida
            cita!.Estado = "Atendida";

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            TempData["Mensaje"] =
                "La visita médica y el diagnóstico se guardaron correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Cargar citas programadas sin visitas registradas
        private async Task CargarCitas()
        {
            var citas = await _context.CitasMedicas
                .Where(c => c.Estado == "Programada" &&
                    !_context.VisitasMedicas.Any(
                        v => v.CitaMedicaId == c.Id))
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var pacientes = await _context.Pacientes.ToListAsync();
            var medicos = await _context.Medicos.ToListAsync();

            var opciones = citas.Select(c =>
            {
                var paciente = pacientes.FirstOrDefault(
                    p => p.Id == c.PacienteId);

                var medico = medicos.FirstOrDefault(
                    m => m.Id == c.MedicoId);

                return new
                {
                    c.Id,
                    Descripcion =
                        (paciente == null
                            ? "Paciente desconocido"
                            : paciente.Nombre + " " + paciente.Apellido)
                        + " - " +
                        (medico == null
                            ? "Médico desconocido"
                            : medico.Nombre + " " + medico.Apellido)
                        + " - " +
                        c.Fecha.ToString("dd/MM/yyyy HH:mm")
                };
            }).ToList();

            ViewBag.ListaCitas = new SelectList(
                opciones, "Id", "Descripcion");
        }
    }
}
