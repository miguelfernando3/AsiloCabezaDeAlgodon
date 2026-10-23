
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;
using AsilioCabezaAlgodon.Models;

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

        // Mostrar visitas médicas registradas
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

        // Mostrar formulario para registrar una visita
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
            // Verificar que la cita exista
            var cita = await _context.CitasMedicas
                .FirstOrDefaultAsync(c => c.Id == modelo.CitaMedicaId);

            if (cita == null)
            {
                ModelState.AddModelError(
                    "CitaMedicaId",
                    "Selecciona una cita médica válida.");
            }
            else
            {
                // Evitar registrar dos visitas para la misma cita
                bool existeVisita = await _context.VisitasMedicas
                    .AnyAsync(v => v.CitaMedicaId == modelo.CitaMedicaId);

                if (existeVisita)
                {
                    ModelState.AddModelError(
                        "CitaMedicaId",
                        "Esta cita ya tiene una visita registrada.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarCitas();
                return View(modelo);
            }

            // Crear la visita médica
            var visita = new VisitaMedica
            {
                CitaMedicaId = modelo.CitaMedicaId,
                Fecha = modelo.Fecha,
                Motivo = modelo.Motivo,
                Diagnostico = modelo.Diagnostico,
                Observaciones = modelo.Observaciones
            };

            // Guardar visita y diagnóstico en una transacción
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            _context.VisitasMedicas.Add(visita);
            await _context.SaveChangesAsync();

            var diagnostico = new Diagnostico
            {
                VisitaMedicaId = visita.Id,
                Descripcion = modelo.Diagnostico,
                Fecha = modelo.Fecha
            };

            _context.Diagnosticos.Add(diagnostico);
            // Cambiar el estado de la cita a Atendida
            cita!.Estado = "Atendida"; 
            await _context.SaveChangesAsync();
            
            // Confirmar todos los cambios
            await transaccion.CommitAsync();

            return RedirectToAction("Index");
        }

        // Cargar citas disponibles para registrar visitas
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
