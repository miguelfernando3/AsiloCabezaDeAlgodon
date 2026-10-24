
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

            return RedirectToAction("Index");
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
