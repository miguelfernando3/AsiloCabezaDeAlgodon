
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Medico,Fundacion")]
    public class SolicitudMedicaController : Controller
    {
        private readonly AsiloContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SolicitudMedicaController> _logger;

        public SolicitudMedicaController(
            AsiloContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<SolicitudMedicaController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Mostrar solicitudes
        public async Task<IActionResult> Index()
        {
            var solicitudes = await _context.SolicitudesMedicas
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            ViewBag.Pacientes = await _context.Pacientes.ToListAsync();

            return View(solicitudes);
        }

        // Formulario de nueva solicitud
        [HttpGet]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create()
        {
            await CargarPacientes();
            return View(new SolicitudMedica());
        }

        // Guardar solicitud y notificar al familiar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Medico")]
        public async Task<IActionResult> Create(SolicitudMedica solicitud)
        {
            solicitud.Id = 0;
            solicitud.FechaSolicitud = DateTime.Now;
            solicitud.Estado = "Pendiente";
            solicitud.CitaMedicaId = null;

            ModelState.Remove(nameof(SolicitudMedica.Id));
            ModelState.Remove(nameof(SolicitudMedica.FechaSolicitud));
            ModelState.Remove(nameof(SolicitudMedica.Estado));
            ModelState.Remove(nameof(SolicitudMedica.CitaMedicaId));

            var paciente = await _context.Pacientes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == solicitud.PacienteId);

            if (paciente == null)
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

            // Primero guardar la solicitud
            _context.SolicitudesMedicas.Add(solicitud);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "La solicitud médica fue guardada correctamente.";

            // Buscar un familiar autorizado
            var familiar = await _context.Familiares
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.PacienteId == solicitud.PacienteId &&
                    f.RecibeNotificaciones);

            if (familiar == null ||
                string.IsNullOrWhiteSpace(familiar.Correo))
            {
                TempData["Aviso"] =
                    "La solicitud fue guardada, pero no hay un familiar autorizado con correo.";
                return RedirectToAction(nameof(Index));
            }

            var correo = familiar.Correo.Trim();

            if (!new EmailAddressAttribute().IsValid(correo))
            {
                TempData["Aviso"] =
                    "La solicitud fue guardada, pero el correo del familiar no es válido.";
                return RedirectToAction(nameof(Index));
            }

            // Al crear la solicitud todavía no hay médico asignado.
            // La fundación lo seleccionará al programar la cita.
            var datos = new
            {
                correoFamiliar = correo,
                nombreFamiliar =
                    familiar.Nombre + " " + familiar.Apellido,
                nombrePaciente =
                    paciente!.Nombre + " " + paciente.Apellido,
                medicoReferido =
                    "Especialidad: " + solicitud.EspecialidadSolicitada +
                    " (médico pendiente de asignación)",
                estadoPaciente =
                    "Se ha creado una solicitud de atención médica especializada."
            };

            try
            {
                // Confirmar que el familiar sigue autorizado
                var autorizado = await _context.Familiares
                    .AsNoTracking()
                    .AnyAsync(f =>
                        f.Id == familiar.Id &&
                        f.PacienteId == solicitud.PacienteId &&
                        f.RecibeNotificaciones &&
                        f.Correo == familiar.Correo);

                if (!autorizado)
                {
                    TempData["Aviso"] =
                        "La solicitud fue guardada, pero cambió la autorización del familiar.";
                    return RedirectToAction(nameof(Index));
                }

                var cliente = _httpClientFactory
                    .CreateClient("Notificaciones");

                var respuesta = await cliente.PostAsJsonAsync(
                    "api/v1/notificar-familiar", datos);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] =
                        "La solicitud médica fue guardada y el correo fue enviado correctamente.";
                }
                else
                {
                    _logger.LogWarning(
                        "Error del microservicio al notificar solicitud {SolicitudId}: {Estado}",
                        solicitud.Id,
                        respuesta.StatusCode);

                    TempData["Aviso"] =
                        "La solicitud fue guardada, pero el servicio de correo no confirmó el envío.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al notificar solicitud médica {SolicitudId}",
                    solicitud.Id);

                TempData["Aviso"] =
                    "La solicitud fue guardada, pero no se pudo enviar el correo. Verifique que Java esté ejecutándose.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Mostrar formulario para programar cita
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
                TempData["Error"] =
                    "Esta solicitud ya fue programada.";

                return RedirectToAction(nameof(Index));
            }

            await CargarMedicos(
                solicitud.EspecialidadSolicitada ?? "");

            ViewBag.Solicitud = solicitud;

            var familiar = await _context.Familiares
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.PacienteId == solicitud.PacienteId &&
                    f.RecibeNotificaciones);

            ViewBag.CorreoDestino = familiar?.Correo ?? "";

            return View(new CitaMedica
            {
                PacienteId = solicitud.PacienteId,
                Motivo = solicitud.Motivo,
                Fecha = DateTime.Now.AddDays(1),
                Estado = "Programada",
                EstadoPago = "Pendiente"
            });
        }

        // Programar cita y notificar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Fundacion")]
        public async Task<IActionResult> Programar(
            int id,
            CitaMedica cita,
            string? correoDestino)
        {
            var solicitud = await _context.SolicitudesMedicas
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
                return NotFound();

            if (solicitud.Estado != "Pendiente" ||
                solicitud.CitaMedicaId != null)
            {
                TempData["Error"] =
                    "Esta solicitud ya fue programada.";

                return RedirectToAction(nameof(Index));
            }

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

            // Validar médico
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

            // Validar destinatario
            var familiar = await _context.Familiares
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.PacienteId == solicitud.PacienteId &&
                    f.RecibeNotificaciones);

            correoDestino = correoDestino?.Trim();

            if (!string.IsNullOrWhiteSpace(correoDestino))
            {
                if (!new EmailAddressAttribute()
                    .IsValid(correoDestino))
                {
                    ModelState.AddModelError(
                        "correoDestino",
                        "Ingrese un correo electrónico válido.");
                }
                else if (familiar == null ||
                    !string.Equals(
                        familiar.Correo?.Trim(),
                        correoDestino,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "correoDestino",
                        "El correo debe coincidir con el familiar autorizado del paciente.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarMedicos(
                    solicitud.EspecialidadSolicitada ?? "");

                ViewBag.Solicitud = solicitud;
                ViewBag.CorreoDestino = correoDestino;

                return View(cita);
            }

            // Guardar cita y solicitud en una transacción
            await using (var transaccion =
                await _context.Database.BeginTransactionAsync())
            {
                int actualizadas = await _context.SolicitudesMedicas
                    .Where(s =>
                        s.Id == id &&
                        s.Estado == "Pendiente" &&
                        s.CitaMedicaId == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Estado, "Procesando"));

                if (actualizadas == 0)
                {
                    await transaccion.RollbackAsync();

                    TempData["Error"] =
                        "La solicitud ya está siendo procesada.";

                    return RedirectToAction(nameof(Index));
                }

                _context.CitasMedicas.Add(cita);
                await _context.SaveChangesAsync();

                await _context.SolicitudesMedicas
                    .Where(s =>
                        s.Id == id &&
                        s.Estado == "Procesando")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Estado, "Programada")
                        .SetProperty(x => x.CitaMedicaId, cita.Id));

                await transaccion.CommitAsync();
            }

            TempData["Mensaje"] =
                "La cita fue programada correctamente.";

            // Enviar aviso de cita programada
            if (!string.IsNullOrWhiteSpace(correoDestino) &&
                familiar != null &&
                medico != null)
            {
                try
                {
                    var autorizado = await _context.Familiares
                        .AsNoTracking()
                        .FirstOrDefaultAsync(f =>
                            f.Id == familiar.Id &&
                            f.PacienteId == solicitud.PacienteId &&
                            f.RecibeNotificaciones &&
                            f.Correo == familiar.Correo);

                    if (autorizado == null)
                    {
                        TempData["Aviso"] =
                            "La cita fue guardada, pero cambió la autorización del familiar.";
                    }
                    else
                    {
                        var paciente = await _context.Pacientes
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p =>
                                p.Id == solicitud.PacienteId);

                        if (paciente == null)
                        {
                            TempData["Aviso"] =
                                "La cita fue guardada, pero no se encontró al paciente.";
                        }
                        else
                        {
                            var datos = new
                            {
                                correoFamiliar = correoDestino,
                                nombreFamiliar =
                                    autorizado.Nombre + " " +
                                    autorizado.Apellido,
                                nombrePaciente =
                                    paciente.Nombre + " " +
                                    paciente.Apellido,
                                medicoReferido =
                                    medico.Nombre + " " +
                                    medico.Apellido + " - " +
                                    medico.Especialidad,
                                estadoPaciente =
                                    "Se ha programado una cita médica."
                            };

                            var cliente = _httpClientFactory
                                .CreateClient("Notificaciones");

                            var respuesta = await cliente.PostAsJsonAsync(
                                "api/v1/notificar-familiar",
                                datos);

                            if (respuesta.IsSuccessStatusCode)
                            {
                                TempData["Mensaje"] =
                                    "La cita fue programada y el correo fue enviado correctamente.";
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Error del microservicio al notificar cita {CitaId}: {Estado}",
                                    cita.Id,
                                    respuesta.StatusCode);

                                TempData["Aviso"] =
                                    "La cita fue guardada, pero el servicio de correo no confirmó el envío.";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error al notificar cita médica {CitaId}",
                        cita.Id);

                    TempData["Aviso"] =
                        "La cita fue guardada, pero no se pudo enviar el correo. Verifique que Java esté ejecutándose.";
                }
            }
            else
            {
                TempData["Aviso"] =
                    "La cita fue guardada sin enviar correo porque no se indicó un destinatario autorizado.";
            }

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
                    NombreCompleto =
                        p.Nombre + " " + p.Apellido
                }),
                "Id",
                "NombreCompleto");
        }

        // Cargar médicos por especialidad
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
                    NombreCompleto =
                        m.Nombre + " " +
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
