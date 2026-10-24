
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

            var pacientes = await _context.Pacientes
                .ToDictionaryAsync(
                    p => p.Id,
                    p => p.Nombre + " " + p.Apellido);

            ViewBag.NombresPacientes = pacientes;

            return View(pagos);
        }

        // Mostrar formulario
        [HttpGet]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> Create()
        {
            await CargarListas();

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

            CitaMedica? cita = null;

            if (pago.CitaMedicaId.HasValue)
            {
                cita = await _context.CitasMedicas
                    .FirstOrDefaultAsync(c =>
                        c.Id == pago.CitaMedicaId.Value);

                if (cita == null)
                {
                    ModelState.AddModelError(
                        "CitaMedicaId",
                        "Seleccione una cita médica válida.");
                }
                else
                {
                    if (cita.PacienteId != pago.PacienteId)
                    {
                        ModelState.AddModelError(
                            "CitaMedicaId",
                            "La cita no pertenece al paciente seleccionado.");
                    }

                    decimal total = cita.CostoConsulta - cita.Descuento;
                    decimal saldo = total - cita.MontoPagado;

                    if (saldo <= 0)
                    {
                        ModelState.AddModelError(
                            "CitaMedicaId",
                            "Esta cita no tiene saldo pendiente.");
                    }
                    else if (pago.Monto > saldo)
                    {
                        ModelState.AddModelError(
                            "Monto",
                            $"El saldo pendiente de la cita es Q{saldo:F2}.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarListas();
                return View(pago);
            }

            // Guardar el pago y actualizar la cita juntos
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Pagos.Add(pago);

                if (cita != null)
                {
                    cita.MontoPagado += pago.Monto;

                    decimal total = cita.CostoConsulta - cita.Descuento;

                    if (cita.MontoPagado >= total)
                    {
                        cita.EstadoPago = "Pagado";
                    }
                    else
                    {
                        cita.EstadoPago = "Parcial";
                    }
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return RedirectToAction("Index");
            }
            catch
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "No se pudo guardar el pago. Inténtelo nuevamente.");

                await CargarListas();
                return View(pago);
            }
        }

        // Cargar pacientes y citas pendientes
        private async Task CargarListas()
        {
            var pacientes = await _context.Pacientes
                .OrderBy(p => p.Nombre)
                .ThenBy(p => p.Apellido)
                .ToListAsync();

            ViewBag.ListaPacientes = new SelectList(
                pacientes.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " " + p.Apellido
                }),
                "Id",
                "NombreCompleto");

            var citas = await _context.CitasMedicas
                .Where(c =>
                    c.CostoConsulta - c.Descuento > c.MontoPagado)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var opciones = citas.Select(c =>
            {
                var paciente = pacientes.FirstOrDefault(
                    p => p.Id == c.PacienteId);

                decimal saldo =
                    c.CostoConsulta - c.Descuento - c.MontoPagado;

                return new
                {
                    c.Id,
                    Descripcion =
                        (paciente == null
                            ? "Paciente no encontrado"
                            : paciente.Nombre + " " + paciente.Apellido)
                        + " - " + c.Fecha.ToString("dd/MM/yyyy")
                        + " - Saldo Q" + saldo.ToString("F2")
                };
            }).ToList();

            ViewBag.ListaCitas = new SelectList(
                opciones,
                "Id",
                "Descripcion");
        }
    }
}
