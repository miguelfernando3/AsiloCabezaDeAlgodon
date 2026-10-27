
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net.Mail;
using AsilioCabezaAlgodon.Datos;
using AsilioCabezaAlgodon.Entidades;

namespace AsilioCabezaAlgodon.Controllers
{
    [Authorize(Roles = "Administrador,Recepcionista,Medico,Fundacion")]
    public class CitaMedicaController : Controller
    {
        private readonly AsiloContext _context;

        public CitaMedicaController(AsiloContext context)
        {
            _context = context;
        }

        // Mostrar todas las citas médicas
        public async Task<IActionResult> Index()
        {
            var citas = await _context.CitasMedicas
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            ViewBag.Pacientes = await _context.Pacientes.ToListAsync();
            ViewBag.Medicos = await _context.Medicos.ToListAsync();

            return View(citas);
        }

        // Mostrar formulario para registrar una cita
        [HttpGet]
        [Authorize(Roles = "Administrador,Recepcionista,Fundacion")]
        public async Task<IActionResult> Create()
        {
            await CargarListas();

            return View(new CitaMedica
            {
                Fecha = DateTime.Now,
                Estado = "Programada",
                CostoConsulta = 0,
                Descuento = 0,
                MontoPagado = 0,
                EstadoPago = "Pendiente"
            });
        }

        // Guardar una cita médica
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Recepcionista,Fundacion")]
        public async Task<IActionResult> Create(CitaMedica cita)
        {
            // El sistema establece el estado de la cita
            cita.Estado = "Programada";

            // El sistema calculará el estado del pago
            cita.EstadoPago = "Pendiente";

            ModelState.Remove(nameof(CitaMedica.Estado));
            ModelState.Remove(nameof(CitaMedica.EstadoPago));

            // Verificar que el paciente exista
            if (!await _context.Pacientes
                .AnyAsync(p => p.Id == cita.PacienteId))
            {
                ModelState.AddModelError(
                    "PacienteId",
                    "Selecciona un paciente válido.");
            }

            // Verificar que el médico exista
            if (!await _context.Medicos
                .AnyAsync(m => m.Id == cita.MedicoId))
            {
                ModelState.AddModelError(
                    "MedicoId",
                    "Selecciona un médico válido.");
            }

            // Verificar la fecha
            if (cita.Fecha == default)
            {
                ModelState.AddModelError(
                    "Fecha",
                    "Selecciona una fecha válida.");
            }

            // Validar costo de consulta
            if (cita.CostoConsulta < 0)
            {
                ModelState.AddModelError(
                    "CostoConsulta",
                    "El costo no puede ser negativo.");
            }

            // Validar descuento
            if (cita.Descuento < 0)
            {
                ModelState.AddModelError(
                    "Descuento",
                    "El descuento no puede ser negativo.");
            }

            if (cita.Descuento > cita.CostoConsulta)
            {
                ModelState.AddModelError(
                    "Descuento",
                    "El descuento no puede superar el costo.");
            }

            // Validar monto pagado
            if (cita.MontoPagado < 0)
            {
                ModelState.AddModelError(
                    "MontoPagado",
                    "El monto pagado no puede ser negativo.");
            }

            // Calcular el total después del descuento
            decimal total = cita.CostoConsulta - cita.Descuento;

            if (cita.MontoPagado > total)
            {
                ModelState.AddModelError(
                    "MontoPagado",
                    "El pago no puede superar el total de la consulta.");
            }

            // Verificar todos los datos
            if (!ModelState.IsValid)
            {
                await CargarListas();
                return View(cita);
            }

            // Calcular automáticamente el estado del pago
            if (total == 0)
            {
                cita.EstadoPago = "Pendiente";
            }
            else if (cita.MontoPagado >= total)
            {
                cita.EstadoPago = "Pagado";
            }
            else if (cita.MontoPagado > 0)
            {
                cita.EstadoPago = "Parcial";
            }
            else
            {
                cita.EstadoPago = "Pendiente";
            }

            // Guardar la cita en SQL Server
            _context.CitasMedicas.Add(cita);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Reenviar notificación de una cita médica
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Recepcionista,Fundacion")]
        public async Task<IActionResult> ReenviarNotificacion(int id)
        {
            // Buscar la cita médica
            var cita = await _context.CitasMedicas
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cita == null)
            {
                return NotFound();
            }

            // Buscar al paciente
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.Id == cita.PacienteId);

            if (paciente == null)
            {
                TempData["Error"] =
                    "No se encontró al paciente.";

                return RedirectToAction(nameof(Index));
            }

            // Buscar al médico
            var medico = await _context.Medicos
                .FirstOrDefaultAsync(m => m.Id == cita.MedicoId);

            if (medico == null)
            {
                TempData["Error"] =
                    "No se encontró al médico.";

                return RedirectToAction(nameof(Index));
            }

            // Buscar al familiar autorizado
            var familiar = await _context.Familiares
                .FirstOrDefaultAsync(f =>
                    f.PacienteId == cita.PacienteId &&
                    f.RecibeNotificaciones);

            if (familiar == null)
            {
                TempData["Error"] =
                    "Este paciente no tiene un familiar autorizado.";

                return RedirectToAction(nameof(Index));
            }

            // Verificar el correo electrónico
            if (string.IsNullOrWhiteSpace(familiar.Correo))
            {
                TempData["Error"] =
                    "El familiar autorizado no tiene correo electrónico.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var correo = new MailAddress(familiar.Correo);

                if (correo.Address != familiar.Correo.Trim())
                {
                    TempData["Error"] =
                        "El correo del familiar no es válido.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (FormatException)
            {
                TempData["Error"] =
                    "El correo del familiar no es válido.";

                return RedirectToAction(nameof(Index));
            }

            // Preparar la información para Java
            var datos = new
            {
                correoFamiliar = familiar.Correo,
                nombreFamiliar = familiar.Nombre + " " +
                                 familiar.Apellido,
                nombrePaciente = paciente.Nombre + " " +
                                 paciente.Apellido,
                medicoReferido = medico.Nombre + " " +
                                 medico.Apellido,
                estadoPaciente = "Cita médica programada para el " +
                    cita.Fecha.ToString("dd/MM/yyyy HH:mm")
            };

            try
            {
                using var cliente = new HttpClient();

                cliente.Timeout = TimeSpan.FromSeconds(15);

                // Enviar información al microservicio Java
                var respuesta = await cliente.PostAsJsonAsync(
                    "http://localhost:8080/api/v1/notificar-familiar",
                    datos);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] =
                        "La notificación fue reenviada correctamente al familiar.";
                }
                else
                {
                    TempData["Error"] =
                        "Java no pudo enviar la notificación. Revise el microservicio.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "No se pudo conectar con el microservicio Java.";
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] =
                    "El microservicio Java tardó demasiado en responder.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Cargar pacientes y médicos en los selectores
        private async Task CargarListas()
        {
            var pacientes = await _context.Pacientes
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            var medicos = await _context.Medicos
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            ViewBag.ListaPacientes = new SelectList(
                pacientes.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " " + p.Apellido
                }),
                "Id",
                "NombreCompleto");

            ViewBag.ListaMedicos = new SelectList(
                medicos.Select(m => new
                {
                    m.Id,
                    NombreCompleto = m.Nombre + " " +
                                     m.Apellido + " - " +
                                     m.Especialidad
                }),
                "Id",
                "NombreCompleto");
        }
    }
}
