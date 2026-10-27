
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
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
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.NombresPacientes = await _context.Pacientes
                .ToDictionaryAsync(
                    p => p.Id,
                    p => p.Nombre + " " + p.Apellido);

            return View(pagos);
        }

        // Mostrar formulario de pagos
        [HttpGet]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> Create()
        {
            await CargarListas();

            return View(new Pago
            {
                Fecha = DateTime.Today,
                EsMensualidad = false
            });
        }

        // Registrar un pago
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> Create(Pago pago)
        {
            // Verificar que el paciente exista
            bool existePaciente = await _context.Pacientes
                .AnyAsync(p => p.Id == pago.PacienteId);

            if (!existePaciente)
            {
                ModelState.AddModelError(
                    nameof(Pago.PacienteId),
                    "Seleccione un paciente válido.");
            }

            // Validar datos de mensualidad
            if (pago.EsMensualidad)
            {
                if (pago.CitaMedicaId.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(Pago.CitaMedicaId),
                        "Una mensualidad no debe relacionarse con una cita médica.");
                }

                if (!pago.Mes.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(Pago.Mes),
                        "Seleccione el mes de la mensualidad.");
                }

                if (!pago.Anio.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(Pago.Anio),
                        "Ingrese el año de la mensualidad.");
                }
            }
            else
            {
                // Los pagos normales no necesitan mes ni año
                pago.Mes = null;
                pago.Anio = null;

                ModelState.Remove(nameof(Pago.Mes));
                ModelState.Remove(nameof(Pago.Anio));
            }

            // Verificar la cita médica si se seleccionó
            CitaMedica? cita = null;

            if (!pago.EsMensualidad && pago.CitaMedicaId.HasValue)
            {
                cita = await _context.CitasMedicas
                    .FirstOrDefaultAsync(c =>
                        c.Id == pago.CitaMedicaId.Value);

                if (cita == null)
                {
                    ModelState.AddModelError(
                        nameof(Pago.CitaMedicaId),
                        "Seleccione una cita médica válida.");
                }
                else
                {
                    if (cita.PacienteId != pago.PacienteId)
                    {
                        ModelState.AddModelError(
                            nameof(Pago.CitaMedicaId),
                            "La cita no pertenece al paciente seleccionado.");
                    }

                    decimal total =
                        Math.Max(0m, cita.CostoConsulta - cita.Descuento);

                    decimal saldo = total - cita.MontoPagado;

                    if (saldo <= 0)
                    {
                        ModelState.AddModelError(
                            nameof(Pago.CitaMedicaId),
                            "Esta cita no tiene saldo pendiente.");
                    }
                    else if (pago.Monto > saldo)
                    {
                        ModelState.AddModelError(
                            nameof(Pago.Monto),
                            $"El saldo pendiente de la cita es Q{saldo:F2}.");
                    }
                }
            }

            // Comprobar datos antes de guardar
            if (!ModelState.IsValid)
            {
                await CargarListas();
                return View(pago);
            }

            // La transacción ayuda a evitar pagos simultáneos
            // que dupliquen mensualidades o superen el saldo.
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                if (pago.EsMensualidad)
                {
                    // Una mensualidad completa por paciente, mes y año
                    bool existeMensualidad = await _context.Pagos
                        .AnyAsync(p =>
                            p.PacienteId == pago.PacienteId &&
                            p.EsMensualidad &&
                            p.Mes == pago.Mes &&
                            p.Anio == pago.Anio);

                    if (existeMensualidad)
                    {
                        await transaccion.RollbackAsync();

                        ModelState.AddModelError(
                            "",
                            "Este paciente ya tiene registrada una mensualidad para ese mes y año.");

                        await CargarListas();
                        return View(pago);
                    }
                }

                if (cita != null)
                {
                    // Consultar nuevamente el saldo dentro
                    // de la transacción antes de guardar.
                    var citaActual = await _context.CitasMedicas
                        .FirstOrDefaultAsync(c => c.Id == cita.Id);

                    if (citaActual == null)
                    {
                        await transaccion.RollbackAsync();

                        ModelState.AddModelError(
                            nameof(Pago.CitaMedicaId),
                            "La cita ya no está disponible.");

                        await CargarListas();
                        return View(pago);
                    }

                    decimal total =
                        Math.Max(0m,
                            citaActual.CostoConsulta - citaActual.Descuento);

                    decimal saldo = total - citaActual.MontoPagado;

                    if (saldo <= 0 || pago.Monto > saldo)
                    {
                        await transaccion.RollbackAsync();

                        ModelState.AddModelError(
                            nameof(Pago.Monto),
                            "El monto supera el saldo disponible de la cita.");

                        await CargarListas();
                        return View(pago);
                    }

                    citaActual.MontoPagado += pago.Monto;

                    citaActual.EstadoPago =
                        citaActual.MontoPagado >= total
                        ? "Pagado"
                        : "Parcial";
                }

                _context.Pagos.Add(pago);

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return RedirectToAction(nameof(Index));
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
