
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Medico,Fundacion")]
    public class SolicitudMedicaController : Controller
    {
        private readonly AsiloContext _context;

        public SolicitudMedicaController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar todas las solicitudes
        public async Task<IActionResult> Index()
        {
            var solicitudes = await _context.SolicitudesMedicas
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            ViewBag.Pacientes = await _context.Pacientes.ToListAsync();

            return View(solicitudes);
        }

        // Mostrar formulario de solicitud
        [HttpGet]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create()
        {
            await CargarPacientes();

            return View(new SolicitudMedica());
        }

        // Guardar solicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create(SolicitudMedica solicitud)
        {
            solicitud.FechaSolicitud = DateTime.Now;
            solicitud.Estado = "Pendiente";
            solicitud.CitaMedicaId = null;

            ModelState.Remove(nameof(SolicitudMedica.Estado));
            ModelState.Remove(nameof(SolicitudMedica.CitaMedicaId));
            ModelState.Remove(nameof(SolicitudMedica.FechaSolicitud));

            if (!await _context.Pacientes
                .AnyAsync(p => p.Id == solicitud.PacienteId))
            {
                ModelState.AddModelError(
                    nameof(SolicitudMedica.PacienteId),
                    "Seleccione un paciente válido.");
            }

            if (!ModelState.IsValid)
            {
                await CargarPacientes();
                return View(solicitud);
            }

            _context.SolicitudesMedicas.Add(solicitud);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Formulario para programar una solicitud
        [HttpGet]
        [Authorize(Roles = "Administrador,Fundacion")]
        public async Task<IActionResult> Programar(int id)
        {
            var solicitud = await _context.SolicitudesMedicas
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
                return NotFound();

            if (solicitud.Estado != "Pendiente" ||
                solicitud.CitaMedicaId != null)
            {
                TempData["Error"] = "Esta solicitud ya fue programada.";
                return RedirectToAction(nameof(Index));
            }

            await CargarMedicos(solicitud.EspecialidadSolicitada);

            ViewBag.Solicitud = solicitud;

            return View(new CitaMedica
            {
                PacienteId = solicitud.PacienteId,
                Motivo = solicitud.Motivo,
                Fecha = DateTime.Now.AddDays(1),
                Estado = "Programada",
                EstadoPago = "Pendiente"
            });
        }

        // Guardar cita programada por la fundación
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Fundacion")]
        public async Task<IActionResult> Programar(
            int id, CitaMedica cita)
        {
            var solicitud = await _context.SolicitudesMedicas
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
                return NotFound();

            if (solicitud.Estado != "Pendiente" ||
                solicitud.CitaMedicaId != null)
            {
                TempData["Error"] = "Esta solicitud ya fue programada.";
                return RedirectToAction(nameof(Index));
            }

            // Estos datos vienen de la solicitud, no del formulario
            cita.Id = 0;
            cita.PacienteId = solicitud.PacienteId;
            cita.Motivo = solicitud.Motivo;
            cita.Estado = "Programada";
            cita.MontoPagado = 0;
            cita.EstadoPago = "Pendiente";

            ModelState.Remove(nameof(CitaMedica.Id));
            ModelState.Remove(nameof(CitaMedica.PacienteId));
            ModelState.Remove(nameof(CitaMedica.Motivo));
            ModelState.Remove(nameof(CitaMedica.Estado));
            ModelState.Remove(nameof(CitaMedica.MontoPagado));
            ModelState.Remove(nameof(CitaMedica.EstadoPago));

            // Validar médico y especialidad
            var medico = await _context.Medicos
                .FirstOrDefaultAsync(m => m.Id == cita.MedicoId);

            if (medico == null)
            {
                ModelState.AddModelError(
                    nameof(CitaMedica.MedicoId),
                    "Seleccione un médico válido.");
            }
            else if (!string.Equals(
                medico.Especialidad?.Trim(),
                solicitud.EspecialidadSolicitada?.Trim(),
                StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(CitaMedica.MedicoId),
                    "El médico no tiene la especialidad solicitada.");
            }

            if (cita.Fecha <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(CitaMedica.Fecha),
                    "La cita debe tener una fecha futura.");
            }

            if (cita.CostoConsulta < 0)
            {
                ModelState.AddModelError(
                    nameof(CitaMedica.CostoConsulta),
                    "El costo no puede ser negativo.");
            }

            if (cita.Descuento < 0 ||
                cita.Descuento > cita.CostoConsulta)
            {
                ModelState.AddModelError(
                    nameof(CitaMedica.Descuento),
                    "El descuento debe estar entre cero y el costo.");
            }

            if (!ModelState.IsValid)
            {
                await CargarMedicos(solicitud.EspecialidadSolicitada ?? "");
                ViewBag.Solicitud = solicitud;
                return View(cita);
            }

            // Guardar cita y actualizar solicitud juntos
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            // Evitar programación duplicada
            int actualizadas = await _context.SolicitudesMedicas
                .Where(s => s.Id == id &&
                            s.Estado == "Pendiente" &&
                            s.CitaMedicaId == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Estado, "Procesando"));

            if (actualizadas == 0)
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "La solicitud ya está siendo procesada.";
                return RedirectToAction(nameof(Index));
            }

            _context.CitasMedicas.Add(cita);
            await _context.SaveChangesAsync();

            // Actualizar sin volver a cargar una copia anterior
            await _context.SolicitudesMedicas
                .Where(s => s.Id == id && s.Estado == "Procesando")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Estado, "Programada")
                    .SetProperty(x => x.CitaMedicaId, cita.Id));

            await transaccion.CommitAsync();

            TempData["Mensaje"] = "La cita fue programada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Cargar pacientes
        private async Task CargarPacientes()
        {
            var pacientes = await _context.Pacientes
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewBag.ListaPacientes = new SelectList(
                pacientes.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " " + p.Apellido
                }),
                "Id",
                "NombreCompleto");
        }

        // Cargar médicos de la especialidad solicitada
        private async Task CargarMedicos(string especialidad)
        {
            var medicos = await _context.Medicos
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            var disponibles = medicos
                .Where(m => string.Equals(
                    m.Especialidad?.Trim(),
                    especialidad?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .Select(m => new
                {
                    m.Id,
                    NombreCompleto = m.Nombre + " " +
                                     m.Apellido + " - " +
                                     m.Especialidad
                })
                .ToList();

            ViewBag.ListaMedicos = new SelectList(
                disponibles,
                "Id",
                "NombreCompleto");
        }
    }
}
